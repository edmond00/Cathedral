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

    /// <summary>Whether the in-flight wait is an `advance` — see the drain block in <see cref="Pump"/>.</summary>
    private bool _draining;

    /// <summary>Remaining preview CONTINUE presses `advance` may make before giving up.</summary>
    private int _drainPressesLeft;

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

                // `advance` keeps pressing the preview box's CONTINUE until the box is gone. The box
                // is generated in segments — goal, then chosen modus mentis, then persona fit — and
                // each press only clears one, so a script that presses once lands mid-stack and finds
                // no actions on screen. Draining here rather than in the command means each press gets
                // a fresh settle before the next.
                if (_draining)
                {
                    var pv = _game.CliNarration?.CliPreview();
                    if (pv is { Active: true, Complete: true } && _drainPressesLeft > 0)
                    {
                        _drainPressesLeft--;
                        CmdClick(new[] { "continue" });
                        RestartWait($"advance ({_drainPressesLeft} press(es) left)");
                        return;
                    }

                    _draining = false;
                    CliMode.Emit(pv is { Active: true }
                        ? "advance done (preview still generating — try a longer timeout)"
                        : "advance done (no preview pending)");
                }
                else
                {
                    CliMode.Emit($"wait done ({_waitDescription})");
                }
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
                case "destinations":CmdDestinations(rest);            break;
                case "click":       CmdClick(rest);                   break;
                case "choose":      CmdChoose(rest);                  break;
                case "travel":      CmdTravel(rest);                  break;
                case "travel-go":   CmdTravelGo();                    break;
                case "manage":      CmdManage(rest);                  break;
                case "select":      CmdSelect(rest);                  break;
                case "key":         CmdKey(rest);                     break;
                case "scroll":      CmdScroll(rest);                  break;
                case "strategy":    CmdStrategy(rest);                break;
                case "fight-end":   CmdFightEnd(rest);                break;
                case "clock":       CmdClock(rest);                   break;
                case "wait":        CmdWait(rest);                    break;
                case "advance":     CmdAdvance(rest);                 break;
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
          clock <days>              DEBUG: push the world clock forward, then heal any wound
                                    whose time has come (the clock otherwise only moves on
                                    travel and work, and wounds take 100-1000 days to close)
          click skill <name>        use a fighting skill by name (see `regions` in a fight)
          click fighter <name>      click a fighter's map cell — the target step for an attack
          click end-turn            end the active fighter's turn (the END TURN button)
          click menu <label>        press a main-menu button (New, Continue, …)
          click button              press the footer button (LEAVE/INTERRUPT/END/CONTINUE)
          click continue            confirm the dice overlay
          click cell <x> <y>        raw terminal cell click (escape hatch)
          choose <n>                answer the visible popup by index
          travel <vertex|name>      plan a route to a world vertex (bypasses 3D picking)
          travel-go                 commit the planned route and set out (the TRAVEL button)
          manage [tab]              open/close the protagonist screen; with a tab name
                                    (Anatomy, Inventory, Memory, Humors, …) open it there
          select [item name]        show a carried item's info panel; bare `select` lists them
          key <escape|…>            send a key
          scroll up|down [n]        scroll the shared history buffer
        Control
          strategy <succeed|fail-dice|fail-plausibility|auto>
                                    pin action outcomes (needs --debug)
          fight-end <victory|death|runaway>
                                    force-resolve a fight to test its transition
          wait [frames]             block until the game settles (no LLM/travel/dice in flight)
          advance [presses] [secs]  settle, then press the preview box's CONTINUE until it is gone
                                    (default up to 8 presses). Use this, not a bare `click continue`,
                                    to get from a keyword click to the action list
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

        // Carrying load, and whether it is currently grounding the party. Weight never blocks a
        // pickup, so this is the only place the constraint is observable before travel is refused.
        if (_game.CliCarryLoad is { } load)
        {
            sb.Append($" carry[{load.Current}/{load.Max}");
            if (load.Blocker != null) sb.Append($" BLOCKED=\"{load.Blocker}\"");
            sb.Append(']');
        }

        if (_game.CliNarration is { } n)
        {
            var s = n.CliSnapshot();
            sb.Append($" narration[loading={s.AnyLoading} dice={(s.DiceActive ? (s.DiceRolling ? "rolling" : "settled") : "none")}");
            sb.Append($" continue={s.ShowContinue} noetic={s.Noetic}/{s.MaxNoetic}");
            // Objects this narration phase has already observed, out of what the node offers. They are
            // withheld from every later choice list of the phase, so a stuck-looking observation that
            // keeps naming the same thing is diagnosable from here.
            sb.Append($" observed={s.Observed}/{s.Observable}");
            // The preview box hides the action list while it is up, which is the commonest reason
            // a script finds nothing clickable. Say so here rather than leaving it to be guessed.
            var pv = n.CliPreview();
            sb.Append($" preview={(pv.Active ? (pv.Complete ? "ready" : "generating") : "none")}");
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

    /// <summary>
    /// <c>clock &lt;days&gt;</c> — push the world clock forward and run the wound-healing sweep.
    ///
    /// <para>
    /// Debug-only, and the only practical way to test healing from a script: the clock advances
    /// solely on travel arrival and work stints, while a wound needs 100–1000 days to close. Unlike
    /// the <c>--advance-days</c> flag, which fires once before anything has happened, this can be
    /// called at the point in a script where the protagonist is actually wounded.
    /// </para>
    /// </summary>
    private void CmdClock(string[] a)
    {
        if (a.Length == 0 || !double.TryParse(a[0], System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double days) || days <= 0)
        {
            CliMode.Emit("error: clock <days>  (positive number)");
            return;
        }

        Narrative.GameClock.Advance(days);
        int closed = _game.CliHealPartyWounds();
        CliMode.Emit($"ok: clock at {Narrative.GameClock.Days:F1} d; {closed} wound(s) closed");
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
            var pv = n.CliPreview();
            if (pv.Active)
            {
                CliMode.Emit($"preview [{pv.Title}]: {pv.Text}");
                if (pv.Complete) { CliMode.Emit("  click continue  (preview)"); any = true; }
                else CliMode.Emit("  (generating…)");
            }
            else if (n.CliPopup() is { } popup)
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
            var dpv = dc.CliPreview();
            if (dpv.Active)
            {
                CliMode.Emit($"preview [{dpv.Title}]: {dpv.Text}");
                if (dpv.Complete) { CliMode.Emit("  click continue  (preview)"); any = true; }
                else CliMode.Emit("  (generating…)");
            }
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

        if (_game.CurrentMode == GameMode.Fighting && _game.CliFight is { } fight)
        {
            CliMode.Emit(fight.CliState());
            foreach (var (name, key, usable) in fight.CliSkills())
                CliMode.Emit($"  click skill \"{name}\"{(usable ? "" : "  (unavailable)")}   [{key}]");
            foreach (var (name, fx, fy, alive, isEnemy) in fight.CliFighters())
                CliMode.Emit($"  click fighter \"{name}\"  ({fx},{fy}) {(isEnemy ? "enemy" : "party")}{(alive ? "" : " DEAD")}");
            CliMode.Emit("  click end-turn");
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

    /// <summary>
    /// Lists where the avatar can travel. Bare, it lists the immediate graph neighbours.
    ///
    /// <para><c>destinations all [filter]</c> lists everything inside the stat-derived travel radius
    /// instead, optionally filtered by biome or location name — <c>destinations all village</c>. The
    /// neighbour list is only ever half a dozen vertices of whatever the spawn point happens to
    /// border, so testing a biome-specific feature otherwise means hunting for a lucky seed.</para>
    /// </summary>
    private void CmdDestinations(string[] a)
    {
        bool all      = a.Length > 0 && a[0].Equals("all", StringComparison.OrdinalIgnoreCase);
        string filter = all ? string.Join(' ', a.Skip(1)) : string.Join(' ', a);

        IEnumerable<int> candidates;
        if (all)
        {
            candidates = _game.CliWorld.EnumerateReachableVertices();
        }
        else
        {
            var graph = _game.CliWorld.GetTravelGraph();
            if (graph == null) { CliMode.Emit("error: no travel graph"); return; }
            candidates = graph.GetConnectedNodes(_game.CliAvatarVertex);
        }

        int shown = 0;
        foreach (int v in candidates)
        {
            var (biome, location, _) = _game.CliWorld.GetDetailedBiomeInfoAt(v);
            string name = location?.Name ?? biome.Name;
            if (filter.Length > 0 && !name.Contains(filter, StringComparison.OrdinalIgnoreCase)) continue;

            bool ok = _game.CliWorld.IsVertexTraversable(v) && !_game.CliWorld.IsOutOfTravelRange(v);
            CliMode.Emit($"  travel {v}  \"{name}\"  {(ok ? "reachable" : "blocked")}");

            // Bounded so `destinations all` on a big world cannot bury the rest of a script's output.
            if (++shown >= 40) { CliMode.Emit("  … (truncated at 40; narrow with a filter)"); break; }
        }

        if (shown == 0)
            CliMode.Emit(filter.Length > 0
                ? $"no {(all ? "in-range" : "connected")} vertex matching \"{filter}\""
                : "no connected vertices");
    }

    // ── Commands: action ──────────────────────────────────────────────────────

    private void CmdClick(string[] a)
    {
        if (a.Length == 0) { CliMode.Emit("error: click <keyword|action|option|skill|fighter|end-turn|menu|button|continue|cell> …"); return; }

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
            case "skill":
            {
                if (a.Length < 2) { CliMode.Emit("error: click skill <name>"); return; }
                var f = _game.CurrentMode == GameMode.Fighting ? _game.CliFight : null;
                if (f == null) { CliMode.Emit("error: not in a fight"); return; }
                string name = string.Join(' ', a[1..]).Trim('"');
                Report(f.CliClickSkill(name), $"used skill \"{name}\"");
                break;
            }
            case "end-turn":
            {
                var f = _game.CurrentMode == GameMode.Fighting ? _game.CliFight : null;
                if (f == null) { CliMode.Emit("error: not in a fight"); return; }
                Report(f.CliEndTurn(), "ended the turn");
                break;
            }
            case "fighter":
            {
                if (a.Length < 2) { CliMode.Emit("error: click fighter <name>"); return; }
                var f = _game.CurrentMode == GameMode.Fighting ? _game.CliFight : null;
                if (f == null) { CliMode.Emit("error: not in a fight"); return; }
                string name = string.Join(' ', a[1..]).Trim('"');
                Report(f.CliClickFighter(name), $"clicked fighter \"{name}\"");
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
                // A fight's dice box has its own Continue, and it blocks the turn until pressed.
                if (_game.CurrentMode == GameMode.Fighting && _game.CliFight is { } cf)
                {
                    Report(cf.CliDiceContinue(), "confirmed dice");
                    return;
                }
                var dc = _game.CliDialogue?.Controller;
                if (dc != null)
                {
                    // Preview box CONTINUE takes precedence over the dice overlay (they never co-occur).
                    if (dc.CliPreview().Active) { Report(dc.CliPreviewContinue(), "preview continue"); return; }
                    Report(dc.CliDiceContinue(), "confirmed dice");
                    return;
                }
                var n = _game.CliNarration;
                if (n == null) { CliMode.Emit("error: not in narration"); return; }
                var preview = n.CliPreviewContinue();
                if (preview.Present)
                {
                    _game.CliClickCell(preview.X, preview.Y);
                    CliMode.Emit("ok: preview continue");
                    return;
                }
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
            // Resolve by location/biome name, nearest first: the vertex under our feet, then immediate
            // neighbours, then anywhere inside travel range.
            //
            // The avatar's own vertex leads because clicking it *enters* the current location rather
            // than planning a route — so after `--start-at village`, `travel village` walks straight
            // in, where searching neighbours first would plan a trip to some other village and leave
            // the script sitting on the travel box waiting for a `travel-go` that never comes.
            string want  = string.Join(' ', a);
            var graph    = _game.CliWorld.GetTravelGraph();
            var searchIn = new[] { _game.CliAvatarVertex }
                .Concat(graph?.GetConnectedNodes(_game.CliAvatarVertex) ?? Enumerable.Empty<int>())
                .Concat(_game.CliWorld.EnumerateReachableVertices());

            // `travel here` enters the location the avatar is already standing on. Worth its own
            // word because it is what most scripts actually want and the alternative — travel to a
            // named biome, travel-go, wait for arrival, then name it again to go in — is four
            // commands and a race with the travel animation.
            if (want.Equals("here", StringComparison.OrdinalIgnoreCase))
            {
                _game.CliClickVertex(_game.CliAvatarVertex);
                CliMode.Emit($"ok: entering the location at vertex {_game.CliAvatarVertex}");
                return;
            }

            target = -1;
            foreach (int v in searchIn)
            {
                var (biome, location, _) = _game.CliWorld.GetDetailedBiomeInfoAt(v);
                string name = location?.Name ?? biome.Name;
                if (!name.Contains(want, StringComparison.OrdinalIgnoreCase)) continue;
                // The avatar's own vertex is always a valid target — range gating applies to going
                // somewhere, not to standing still.
                if (v != _game.CliAvatarVertex &&
                    (!_game.CliWorld.IsVertexTraversable(v) || _game.CliWorld.IsOutOfTravelRange(v))) continue;
                target = v;
                break;
            }
            if (target < 0) { CliMode.Emit($"error: no reachable destination matching \"{want}\" (try `destinations all`)"); return; }
        }

        _game.CliClickVertex(target);

        // Two quite different things happen here and the script has to know which. Picking your own
        // vertex walks straight into the location; picking any other only *plans* a route and leaves
        // the travel box up, waiting for `travel-go`. Reporting both as "travel requested" is how a
        // script ends up parked at the travel box until its timeout, with nothing saying why.
        if (target == _game.CliAvatarVertex)
            CliMode.Emit($"ok: entering the location at vertex {target} (own vertex)");
        else
            CliMode.Emit($"ok: route planned to vertex {target} — call `travel-go` to set out");
    }

    /// <summary>
    /// Commits the planned waypoints and sets out — the CLI equivalent of pressing TRAVEL.
    /// <c>travel</c> alone only plans a route; without this a script could never leave the
    /// starting vertex, and the carrying-weight gate on departure would be untestable.
    /// </summary>
    private void CmdTravelGo()
    {
        if (_game.CurrentMode != GameMode.WorldView)
        { CliMode.Emit($"error: travel-go only works in WorldView (currently {_game.CurrentMode})"); return; }

        if (_game.CliCarryLoad is { Blocker: { } blocked })
        { CliMode.Emit($"refused: {blocked}"); return; }

        _game.StartPlannedTravel();
        CliMode.Emit($"ok: travelling (mode={_game.CurrentMode})");
    }

    /// <summary>
    /// Opens the protagonist-management screen, or closes it if already open; with a tab name,
    /// opens it and switches to that tab. This is the only way a script can reach the inventory —
    /// in play the screen sits behind the main-menu overlay, which is not clickable from narration.
    /// </summary>
    private void CmdManage(string[] a)
    {
        string? tab = a.Length > 0 ? a[0] : null;

        if (tab != null && _game.CliManagementTabs.Count == 0)
        {
            // Not open yet — open it first so the tab switch has something to act on.
            if (!_game.CliToggleManagement())
            {
                CliMode.Emit("error: no protagonist yet — start a game first");
                return;
            }
        }
        else if (tab == null)
        {
            if (!_game.CliToggleManagement())
            {
                CliMode.Emit("error: no protagonist yet — start a game first");
                return;
            }
            CliMode.Emit("ok: toggled protagonist management");
            return;
        }

        if (!_game.CliSelectManagementTab(tab!))
        {
            CliMode.Emit($"error: unknown tab '{tab}' (have: {string.Join(", ", _game.CliManagementTabs)})");
            return;
        }
        CliMode.Emit($"ok: management tab {tab}");
    }

    /// <summary>
    /// Selects a carried item by name and shows its info panel. Without an argument, lists what is
    /// carried. Reaching the panel otherwise means clicking a body-art box whose position shifts
    /// with the art, so this is the stable handle.
    /// </summary>
    private void CmdSelect(string[] a)
    {
        if (a.Length == 0)
        {
            var carried = _game.CliCarriedItemNames;
            if (carried.Count == 0) { CliMode.Emit("error: open the protagonist screen first (`manage Inventory`)"); return; }
            foreach (var n in carried) CliMode.Emit($"  select \"{n}\"");
            return;
        }

        string name = string.Join(' ', a);
        if (!_game.CliSelectItem(name))
        {
            CliMode.Emit($"error: no carried item matching \"{name}\" (try `select` for the list)");
            return;
        }
        CliMode.Emit($"ok: selected \"{name}\"");
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

    /// <summary>
    /// Waits for the game to settle, then presses the preview box's CONTINUE until no box is left.
    ///
    /// <para>The narration preview arrives in segments — the goal, the modus mentis chosen for it,
    /// the persona's willingness — and CONTINUE clears one segment at a time. A script that presses
    /// once lands in the middle of the stack, where <c>click action</c> reports nothing on screen and
    /// nothing explains why. This is the "press CONTINUE until the game gives me the actions back"
    /// loop a person does without thinking about it.</para>
    /// </summary>
    private void CmdAdvance(string[] a)
    {
        int maxPresses = a.Length >= 1 && int.TryParse(a[0], out int n) ? Math.Max(1, n) : 8;
        int secs       = a.Length >= 2 && int.TryParse(a[1], out int s) ? Math.Max(1, s) : 90;

        _draining         = true;
        _drainPressesLeft = maxPresses;
        RestartWait($"advance (≤{maxPresses} press(es))", TimeSpan.FromSeconds(secs));
    }

    /// <summary>Re-arms the frame-driven wait, keeping whatever drain state is in flight.</summary>
    private void RestartWait(string description, TimeSpan? timeout = null)
    {
        _idleFramesRequired = IdleFramesToSettle;
        _waitCondition      = null;
        _waitDescription    = description;
        _idleFramesSeen     = 0;
        _waitDeadline       = DateTime.UtcNow + (timeout ?? DefaultWaitTimeout);
        _waiting            = true;
    }

    private void CmdWait(string[] a)
    {
        _draining = false;
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
