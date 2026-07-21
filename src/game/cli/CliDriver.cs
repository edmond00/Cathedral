using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Cathedral.Terminal;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Cathedral.Game.Cli;

/// <summary>
/// Reads commands from stdin (and optionally a script file) and applies them to the running game.
///
/// <para><b>Threading.</b> stdin is read on a background thread and commands are queued; they are
/// executed from <see cref="Pump"/> on the game's update tick. Executing them on the reader thread
/// would race every piece of game state, and reading stdin on the update tick would stall rendering.</para>
///
/// <para><b>Design.</b> Commands name things, not coordinates — <c>click keyword hearth</c>, not
/// <c>click 34 12</c>. Coordinate-based scripts break on every layout change and are unwritable
/// without seeing the screen. Use <c>regions</c> to discover what is actionable right now.</para>
///
/// <para><b>Output.</b> Every response is prefixed <c>[cli]</c> so it can be separated from the
/// game's own logging, which shares stdout.</para>
/// </summary>
public sealed class CliDriver
{
    private readonly LocationTravelGameController _game;
    private readonly ConcurrentQueue<string> _queue = new();
    private Thread? _reader;

    /// <summary>Frames still to wait before the next command runs (set by <c>wait</c>).</summary>
    private int _idleFramesRequired;
    private int _idleFramesSeen;
    private bool _waiting;

    /// <summary>
    /// Wall-clock deadline for the current wait. Deliberately not a frame count: the update loop
    /// runs anywhere from 8 to 60 fps depending on what is being rendered, so a frame budget is
    /// minutes in one run and seconds in another.
    /// </summary>
    private DateTime _waitDeadline;

    /// <summary>
    /// Extra condition a <c>wait</c> must satisfy on top of idleness — e.g. "mode is
    /// LocationInteraction". Needed because parts of startup advance on their own: the game can be
    /// perfectly idle at ProtagonistCreation while still on its way to narration.
    /// </summary>
    private Func<bool>? _waitCondition;
    private string _waitDescription = "idle";

    /// <summary>Commands deferred until the current <c>wait</c> completes.</summary>
    private readonly Queue<string> _deferred = new();

    /// <summary>Frames of consecutive idleness required before `wait` returns.</summary>
    private const int IdleFramesToSettle = 3;

    /// <summary>Give up on a `wait` after this long rather than hanging the run.</summary>
    private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Hard deadline for the whole run; the game closes itself when it passes.</summary>
    private DateTime _runDeadline;

    public CliDriver(LocationTravelGameController game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _runDeadline = DateTime.UtcNow + CliMode.RunTimeout;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public void Start()
    {
        if (CliMode.ScriptPath is { } path)
        {
            if (File.Exists(path))
            {
                foreach (var line in File.ReadAllLines(path)) _queue.Enqueue(line);
                CliMode.Emit($"script loaded: {path}");
            }
            else
            {
                CliMode.Emit($"error: script not found: {path}");
            }
        }

        _reader = new Thread(ReadLoop) { IsBackground = true, Name = "cli-stdin" };
        _reader.Start();

        CliMode.Emit("ready — type `help` for commands");
    }

    private void ReadLoop()
    {
        try
        {
            string? line;
            while ((line = Console.ReadLine()) != null)
                _queue.Enqueue(line);
        }
        catch (Exception ex)
        {
            CliMode.Emit($"error: stdin closed ({ex.GetType().Name})");
        }
    }

    /// <summary>
    /// Drain and execute queued commands. Call once per update tick, on the game thread.
    /// </summary>
    public void Pump()
    {
        if (DateTime.UtcNow >= _runDeadline)
        {
            CliMode.HasFailedAssertion = true;
            CliMode.Emit($"FAIL: run timeout ({CliMode.RunTimeout.TotalSeconds:F0}s) — closing. mode={_game.CurrentMode}");
            _runDeadline = DateTime.MaxValue;   // only fire once
            _game.CliRequestClose();
            return;
        }

        // An in-flight `wait` gates everything behind it, so a script reads as a straight sequence.
        if (_waiting)
        {
            bool satisfied = _game.CliIsIdle() && (_waitCondition?.Invoke() ?? true);
            if (satisfied) _idleFramesSeen++;
            else           _idleFramesSeen = 0;

            if (_idleFramesSeen >= _idleFramesRequired)
            {
                _waiting = false;
                CliMode.Emit($"wait done ({_waitDescription})");
            }
            else if (DateTime.UtcNow >= _waitDeadline)
            {
                // Never hang: report and fall through so the script reaches its `quit`.
                _waiting = false;
                CliMode.HasFailedAssertion = true;
                CliMode.Emit($"FAIL: wait timed out ({_waitDescription}; mode={_game.CurrentMode}, idle={_game.CliIsIdle()})");
            }
            else
            {
                return;
            }
        }

        while (_deferred.Count > 0)
        {
            Execute(_deferred.Dequeue());
            if (_waiting) return;   // a deferred command started another wait
        }

        while (_queue.TryDequeue(out var line))
        {
            Execute(line);
            if (_waiting)
            {
                // Park the rest of this batch behind the wait.
                while (_queue.TryDequeue(out var rest)) _deferred.Enqueue(rest);
                return;
            }
        }
    }

    // ── Dispatch ──────────────────────────────────────────────────────────────

    private void Execute(string raw)
    {
        string line = raw.Trim();
        if (line.Length == 0 || line.StartsWith('#')) return;

        CliMode.Emit($"> {line}");

        var parts = Tokenize(line);
        string cmd = parts[0].ToLowerInvariant();
        var rest = parts.Skip(1).ToArray();

        try
        {
            switch (cmd)
            {
                case "help":        CmdHelp();                        break;
                case "state":       CmdState();                       break;
                case "dump":        CmdDump(rest);                    break;
                case "regions":     CmdRegions();                     break;
                case "world":       CmdWorld();                       break;
                case "destinations":CmdDestinations();                break;
                case "click":       CmdClick(rest);                   break;
                case "choose":      CmdChoose(rest);                  break;
                case "travel":      CmdTravel(rest);                  break;
                case "key":         CmdKey(rest);                     break;
                case "scroll":      CmdScroll(rest);                  break;
                case "strategy":    CmdStrategy(rest);                break;
                case "fight-end":   CmdFightEnd(rest);                break;
                case "wait":        CmdWait(rest);                    break;
                case "expect":      CmdExpect(rest, expectPresent: true);  break;
                case "expect-not":  CmdExpect(rest, expectPresent: false); break;
                case "quit":        CmdQuit();                        break;
                default:            CliMode.Emit($"error: unknown command '{cmd}' (try `help`)"); break;
            }
        }
        catch (Exception ex)
        {
            CliMode.Emit($"error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>Split on whitespace, honouring "double quoted" arguments.</summary>
    private static string[] Tokenize(string line)
    {
        var tokens = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        foreach (char c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (!inQuotes && char.IsWhiteSpace(c))
            {
                if (sb.Length > 0) { tokens.Add(sb.ToString()); sb.Clear(); }
                continue;
            }
            sb.Append(c);
        }
        if (sb.Length > 0) tokens.Add(sb.ToString());
        return tokens.Count > 0 ? tokens.ToArray() : new[] { "" };
    }

    // ── Commands: observation ─────────────────────────────────────────────────

    private static void CmdHelp() => CliMode.EmitBlock("""
        Observation
          state                     current mode + phase flags (loading, dice, noetic, …)
          dump [--color]            the terminal grid as text; --color annotates greyed lines
          regions                   what is actionable right now (the handles `click` accepts)
          world                     avatar vertex, biome, location, travel range
          destinations              reachable vertices, by name
        Action
          click keyword <name>      click a narration keyword
          click action <n>          click a narration action by index
          click option <n>          pick a dialogue reply by index
          click menu <label>        press a main-menu button (New, Continue, …)
          click button              press the footer button (LEAVE/INTERRUPT/END/CONTINUE)
          click continue            confirm the dice overlay
          click cell <x> <y>        raw terminal cell click (escape hatch)
          choose <n>                answer the visible popup by index
          travel <vertex|name>      travel to a world vertex (bypasses 3D picking)
          key <escape|…>            send a key
          scroll up|down [n]        scroll the shared history buffer
        Control
          strategy <succeed|fail-dice|fail-plausibility|auto>
                                    pin action outcomes (needs --debug)
          fight-end <victory|death|runaway>
                                    force-resolve a fight to test its transition
          wait [frames]             block until the game settles (no LLM/travel/dice in flight)
          wait mode <GameMode>      block until the game reaches a mode (e.g. LocationInteraction);
                                    a timeout is reported as FAIL
          expect <text>             assert text is on screen; failure sets a non-zero exit code
          expect-not <text>         assert text is absent
          quit                      close the game (exit 1 if any expect failed)
        """);

    private void CmdState()
    {
        var sb = new StringBuilder();
        sb.Append($"mode={_game.CurrentMode} idle={_game.CliIsIdle()}");

        if (_game.CliNarration is { } n)
        {
            var s = n.CliSnapshot();
            sb.Append($" narration[loading={s.AnyLoading} dice={(s.DiceActive ? (s.DiceRolling ? "rolling" : "settled") : "none")}");
            sb.Append($" continue={s.ShowContinue} noetic={s.Noetic}/{s.MaxNoetic}");
            sb.Append($" history={n.ScrollBuffer.HistoryLineCount} total={n.ScrollBuffer.TotalLines}");
            sb.Append($" scroll={n.ScrollBuffer.ScrollOffset}");
            if (s.Error != null) sb.Append($" error=\"{s.Error}\"");
            if (s.AnyLoading)    sb.Append($" msg=\"{s.LoadingMessage}\"");
            sb.Append(']');
        }

        if (_game.CliDialogue is { } d)
        {
            if (d.Controller is { } dc)
            {
                var s = dc.CliSnapshot();
                sb.Append($" dialogue[npc={d.TargetNpc.DisplayName} loading={s.Loading}");
                sb.Append($" dice={(s.DiceActive ? (s.DiceRolling ? "rolling" : "settled") : "none")}");
                sb.Append($" ended={s.Ended} options={s.OptionCount}]");
            }
            else sb.Append(" dialogue[starting…]");
        }

        if (_game.CliFight is { } f)
            sb.Append($" fight[enemy={f.TargetNpc.DisplayName} over={f.IsOver} result={f.Result}]");

        CliMode.Emit(sb.ToString());
    }

    private void CmdDump(string[] args)
    {
        var term = _game.CliTerminal;
        if (term == null) { CliMode.Emit("error: no terminal"); return; }

        bool color = args.Contains("--color");
        var view = term.View;
        var sb = new StringBuilder();

        for (int y = 0; y < view.Height; y++)
        {
            var row = new StringBuilder();
            bool anyDim = false, anyBright = false;
            for (int x = 0; x < view.Width; x++)
            {
                var cell = view[x, y];
                row.Append(cell.Character == '\0' ? ' ' : cell.Character);
                if (cell.Character != ' ' && cell.Character != '\0')
                {
                    // Greyed history renders at a low, near-neutral luminance.
                    float lum = (cell.TextColor.X + cell.TextColor.Y + cell.TextColor.Z) / 3f;
                    if (lum < 0.40f) anyDim = true; else anyBright = true;
                }
            }
            string text = row.ToString().TrimEnd();
            if (color && text.Length > 0)
            {
                string tag = anyDim && !anyBright ? "dim " : anyDim ? "mix " : "lit ";
                sb.Append(tag);
            }
            sb.AppendLine(text);
        }

        CliMode.EmitBlock(sb.ToString().TrimEnd());
    }

    private void CmdRegions()
    {
        bool any = false;

        if (_game.CliMenuButtons() is { } menu)
        {
            foreach (var (label, enabled, _, _) in menu)
                CliMode.Emit($"  click menu \"{label}\"{(enabled ? "" : "  (disabled)")}");
            return;
        }

        if (_game.CliCreationContinue() != null)
        {
            CliMode.Emit("  click continue  (accept the generated protagonist and start the game)");
            return;
        }

        if (_game.CliNarration is { } n)
        {
            if (n.CliPopup() is { } popup)
            {
                CliMode.Emit($"popup ({popup.Kind}):");
                for (int i = 0; i < popup.Labels.Count; i++)
                    CliMode.Emit($"  choose {i}  {popup.Labels[i]}");
                any = true;
            }
            else
            {
                var keywords = n.CliKeywords();
                if (keywords.Count > 0)
                {
                    CliMode.Emit("keywords: " + string.Join(", ", keywords.Select(k => $"\"{k}\"")));
                    any = true;
                }
                foreach (var (idx, text) in n.CliActions())
                {
                    CliMode.Emit($"  click action {idx}  {text}");
                    any = true;
                }
                var dice = n.CliDiceContinue();
                if (dice.Present) { CliMode.Emit("  click continue  [ Continue ]  (dice)"); any = true; }
                var exit = n.CliExitButton();
                if (exit.Present) { CliMode.Emit("  click button  (footer)"); any = true; }
            }
        }

        if (_game.CliDialogue?.Controller is { } dc)
        {
            foreach (var (i, skill, text) in dc.CliOptions())
            {
                CliMode.Emit($"  click option {i}  [{skill}] \"{text}\"");
                any = true;
            }
            var s = dc.CliSnapshot();
            if (s.DiceActive && !s.DiceRolling) { CliMode.Emit("  click continue  (dice)"); any = true; }
            CliMode.Emit("  click button  (END / INTERRUPT)");
            any = true;
        }

        if (_game.CurrentMode == GameMode.WorldView)
        {
            CliMode.Emit("  travel <vertex|name>  — see `destinations`");
            any = true;
        }

        if (!any) CliMode.Emit("no interactable regions (try `wait`, then `regions` again)");
    }

    private void CmdWorld()
    {
        int v = _game.CliAvatarVertex;
        var (location, biome) = _game.CliWorld.GetCurrentLocationInfo();
        CliMode.Emit($"avatar_vertex={v} biome=\"{biome.Name}\" location=\"{location?.Name ?? "-"}\" mode={_game.CurrentMode}");
    }

    private void CmdDestinations()
    {
        var graph = _game.CliWorld.GetTravelGraph();
        if (graph == null) { CliMode.Emit("error: no travel graph"); return; }

        int from = _game.CliAvatarVertex;
        var neighbours = graph.GetConnectedNodes(from).ToList();
        if (neighbours.Count == 0) { CliMode.Emit("no connected vertices"); return; }

        foreach (int v in neighbours)
        {
            var (biome, location, _) = _game.CliWorld.GetDetailedBiomeInfoAt(v);
            bool ok = _game.CliWorld.IsVertexTraversable(v) && !_game.CliWorld.IsOutOfTravelRange(v);
            string name = location?.Name ?? biome.Name;
            CliMode.Emit($"  travel {v}  \"{name}\"  {(ok ? "reachable" : "blocked")}");
        }
    }

    // ── Commands: action ──────────────────────────────────────────────────────

    private void CmdClick(string[] a)
    {
        if (a.Length == 0) { CliMode.Emit("error: click <keyword|action|option|button|continue|cell> …"); return; }

        switch (a[0].ToLowerInvariant())
        {
            case "keyword":
            {
                if (a.Length < 2) { CliMode.Emit("error: click keyword <name>"); return; }
                var n = _game.CliNarration;
                if (n == null) { CliMode.Emit("error: not in narration"); return; }
                Report(n.CliClickKeyword(a[1]), $"clicked keyword \"{a[1]}\"");
                break;
            }
            case "action":
            {
                if (a.Length < 2 || !int.TryParse(a[1], out int idx)) { CliMode.Emit("error: click action <n>"); return; }
                var n = _game.CliNarration;
                if (n == null) { CliMode.Emit("error: not in narration"); return; }
                Report(n.CliClickAction(idx), $"clicked action {idx}");
                break;
            }
            case "option":
            {
                if (a.Length < 2 || !int.TryParse(a[1], out int idx)) { CliMode.Emit("error: click option <n>"); return; }
                var dc = _game.CliDialogue?.Controller;
                if (dc == null) { CliMode.Emit("error: not in a conversation"); return; }
                Report(dc.CliSelectOption(idx), $"picked option {idx}");
                break;
            }
            case "button":
            {
                var dc = _game.CliDialogue?.Controller;
                if (dc != null) { dc.CliPressExitButton(); CliMode.Emit("ok: pressed dialogue footer button"); return; }
                var n = _game.CliNarration;
                if (n == null) { CliMode.Emit("error: no footer button here"); return; }
                var btn = n.CliExitButton();
                if (!btn.Present) { CliMode.Emit("error: no footer button on screen"); return; }
                _game.CliClickCell(btn.X, btn.Y);
                CliMode.Emit("ok: pressed narration footer button");
                break;
            }
            case "continue":
            {
                // Protagonist creation blocks startup until its Continue is pressed.
                if (_game.CliCreationContinue() is { } cc)
                {
                    _game.CliHoverCell(cc.X, cc.Y);
                    _game.CliClickCell(cc.X, cc.Y);
                    CliMode.Emit("ok: accepted protagonist");
                    return;
                }
                var dc = _game.CliDialogue?.Controller;
                if (dc != null) { Report(dc.CliDiceContinue(), "confirmed dice"); return; }
                var n = _game.CliNarration;
                if (n == null) { CliMode.Emit("error: not in narration"); return; }
                var dice = n.CliDiceContinue();
                if (!dice.Present) { CliMode.Emit("error: no dice continue button on screen"); return; }
                _game.CliClickCell(dice.X, dice.Y);
                CliMode.Emit("ok: confirmed dice");
                break;
            }
            case "menu":
            {
                if (a.Length < 2) { CliMode.Emit("error: click menu <label>"); return; }
                var menu = _game.CliMenuButtons();
                if (menu == null) { CliMode.Emit("error: not on the main menu"); return; }
                var match = menu.FirstOrDefault(b => b.Label.Contains(a[1], StringComparison.OrdinalIgnoreCase));
                if (match.Label == null) { CliMode.Emit($"error: no menu button matching \"{a[1]}\""); return; }
                if (!match.Enabled)      { CliMode.Emit($"error: menu button \"{match.Label}\" is disabled"); return; }
                _game.CliHoverCell(match.X, match.Y);
                _game.CliClickCell(match.X, match.Y);
                CliMode.Emit($"ok: pressed menu \"{match.Label}\"");
                break;
            }
            case "cell":
            {
                if (a.Length < 3 || !int.TryParse(a[1], out int x) || !int.TryParse(a[2], out int y))
                { CliMode.Emit("error: click cell <x> <y>"); return; }
                _game.CliHoverCell(x, y);
                _game.CliClickCell(x, y);
                CliMode.Emit($"ok: clicked cell {x},{y}");
                break;
            }
            default:
                CliMode.Emit($"error: unknown click target '{a[0]}'");
                break;
        }
    }

    private void CmdChoose(string[] a)
    {
        if (a.Length < 1 || !int.TryParse(a[0], out int idx)) { CliMode.Emit("error: choose <n>"); return; }
        var n = _game.CliNarration;
        if (n == null) { CliMode.Emit("error: not in narration"); return; }
        Report(n.CliChoosePopup(idx), $"chose {idx}");
    }

    private void CmdTravel(string[] a)
    {
        if (a.Length == 0) { CliMode.Emit("error: travel <vertex|name>"); return; }
        if (_game.CurrentMode != GameMode.WorldView)
        { CliMode.Emit($"error: travel only works in WorldView (currently {_game.CurrentMode})"); return; }

        int target;
        if (int.TryParse(a[0], out int explicitVertex))
        {
            target = explicitVertex;
        }
        else
        {
            // Resolve by location/biome name among connected vertices.
            var graph = _game.CliWorld.GetTravelGraph();
            if (graph == null) { CliMode.Emit("error: no travel graph"); return; }
            string want = a[0];
            target = -1;
            foreach (int v in graph.GetConnectedNodes(_game.CliAvatarVertex))
            {
                var (biome, location, _) = _game.CliWorld.GetDetailedBiomeInfoAt(v);
                string name = location?.Name ?? biome.Name;
                if (name.Contains(want, StringComparison.OrdinalIgnoreCase)) { target = v; break; }
            }
            if (target < 0) { CliMode.Emit($"error: no reachable destination matching \"{want}\" (try `destinations`)"); return; }
        }

        _game.CliClickVertex(target);
        CliMode.Emit($"ok: travel requested to vertex {target}");
    }

    private void CmdKey(string[] a)
    {
        if (a.Length == 0) { CliMode.Emit("error: key <name>"); return; }
        if (!Enum.TryParse<Keys>(a[0], ignoreCase: true, out var key))
        { CliMode.Emit($"error: unknown key '{a[0]}'"); return; }
        _game.OnKeyDown(key);
        CliMode.Emit($"ok: sent {key}");
    }

    private void CmdScroll(string[] a)
    {
        string dir = a.Length > 0 ? a[0].ToLowerInvariant() : "down";
        int n = a.Length > 1 && int.TryParse(a[1], out int parsed) ? parsed : 1;
        for (int i = 0; i < n; i++) _game.OnMouseWheel(dir == "up" ? 1f : -1f);
        CliMode.Emit($"ok: scrolled {dir} x{n}");
    }

    // ── Commands: control ─────────────────────────────────────────────────────

    private static void CmdStrategy(string[] a)
    {
        if (a.Length == 0) { CliMode.Emit($"strategy={DebugMode.CurrentStrategy}"); return; }
        var parsed = DebugMode.ParseStrategy(a[0]);
        if (parsed == null) { CliMode.Emit($"error: unknown strategy '{a[0]}'"); return; }
        DebugMode.SetStrategy(parsed.Value);
        if (!DebugMode.IsActive)
            CliMode.Emit("warning: --debug is not enabled, so action outcomes are not overridden");
        CliMode.Emit($"ok: strategy={parsed.Value}");
    }

    private void CmdFightEnd(string[] a)
    {
        var fight = _game.CliFight;
        if (fight == null) { CliMode.Emit("error: no fight in progress"); return; }
        string want = a.Length > 0 ? a[0].ToLowerInvariant() : "victory";
        Fight.FightResult result = want switch
        {
            "victory" or "win"   => Fight.FightResult.PartyWon,
            "death" or "lose"    => Fight.FightResult.EnemyWon,
            "runaway" or "flee"  => Fight.FightResult.PartyFled,
            _                    => Fight.FightResult.Ongoing,
        };
        if (result == Fight.FightResult.Ongoing)
        { CliMode.Emit($"error: unknown result '{want}' (victory|death|runaway)"); return; }

        fight.CliForceEnd(result);
        CliMode.Emit($"ok: fight force-ended as {result}");
    }

    private void CmdWait(string[] a)
    {
        _idleFramesRequired = IdleFramesToSettle;
        _waitCondition = null;
        _waitDescription = "idle";

        var timeout = DefaultWaitTimeout;

        if (a.Length >= 2 && a[0].Equals("mode", StringComparison.OrdinalIgnoreCase))
        {
            if (!Enum.TryParse<GameMode>(a[1], ignoreCase: true, out var mode))
            { CliMode.Emit($"error: unknown mode '{a[1]}'"); return; }
            _waitCondition = () => _game.CurrentMode == mode;
            _waitDescription = $"mode={mode}";
            if (a.Length >= 3 && int.TryParse(a[2], out int modeSecs))
                timeout = TimeSpan.FromSeconds(Math.Max(1, modeSecs));
        }
        else if (a.Length >= 1 && int.TryParse(a[0], out int secs))
        {
            timeout = TimeSpan.FromSeconds(Math.Max(1, secs));
            _waitDescription = $"idle (≤{secs}s)";
        }

        _idleFramesSeen = 0;
        _waitDeadline = DateTime.UtcNow + timeout;
        _waiting = true;
    }

    private void CmdExpect(string[] a, bool expectPresent)
    {
        if (a.Length == 0) { CliMode.Emit("error: expect <text>"); return; }
        string needle = string.Join(' ', a);

        var term = _game.CliTerminal;
        if (term == null) { CliMode.Emit("error: no terminal"); return; }

        var view = term.View;
        var sb = new StringBuilder();
        for (int y = 0; y < view.Height; y++)
        {
            for (int x = 0; x < view.Width; x++)
            {
                var c = view[x, y].Character;
                sb.Append(c == '\0' ? ' ' : c);
            }
            sb.Append('\n');
        }

        bool found = sb.ToString().Contains(needle, StringComparison.OrdinalIgnoreCase);
        if (found == expectPresent)
        {
            CliMode.Emit($"PASS: {(expectPresent ? "found" : "absent")} \"{needle}\"");
        }
        else
        {
            CliMode.HasFailedAssertion = true;
            CliMode.Emit($"FAIL: expected {(expectPresent ? "to find" : "NOT to find")} \"{needle}\"");
        }
    }

    private void CmdQuit()
    {
        CliMode.Emit(CliMode.HasFailedAssertion ? "quitting (assertions FAILED)" : "quitting (all assertions passed)");
        _game.CliRequestClose();
    }

    private static void Report(string? error, string okMessage)
        => CliMode.Emit(error == null ? $"ok: {okMessage}" : $"error: {error}");
}
