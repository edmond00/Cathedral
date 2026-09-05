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
                    var pv = ActivePreview();
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
                case "world-regions": CmdWorldRegions(rest);         break;
                case "destinations":CmdDestinations(rest);            break;
                case "click":       CmdClick(rest);                   break;
                case "point":       CmdPoint(rest);                   break;
                case "choose":      CmdChoose(rest);                  break;
                case "travel":      CmdTravel(rest);                  break;
                case "travel-go":   CmdTravelGo();                    break;
                case "routines":    CmdRoutines(rest);                break;
                case "manage":      CmdManage(rest);                  break;
                case "select":      CmdSelect(rest);                  break;
                case "key":         CmdKey(rest);                     break;
                case "scroll":      CmdScroll(rest);                  break;
                case "strategy":    CmdStrategy(rest);                break;
                case "goal":        CmdGoal(rest);                    break;
                case "observe":     CmdObserve(rest);                 break;
                case "fight-end":   CmdFightEnd(rest);                break;
                case "fight-deplete": CmdFightDeplete(rest);          break;
                case "fight-wound": CmdFightWound(rest);              break;
                case "wound":       CmdWound(rest);                   break;
                case "cripple":     CmdCripple(rest);                 break;
                case "starve":      CmdStarve(rest);                  break;
                case "clock":       CmdClock(rest);                   break;
                case "wait":        CmdWait(rest);                    break;
                case "advance":     CmdAdvance(rest);                 break;
                case "expect":      CmdExpect(rest, expectPresent: true);  break;
                case "expect-not":  CmdExpect(rest, expectPresent: false); break;
                case "expect-verb": CmdExpectVerb(rest);              break;
                case "inspect":     CmdInspect(rest);                  break;
                case "expect-state":    CmdExpectState(rest, want: true);  break;
                case "expect-no-state": CmdExpectState(rest, want: false); break;
                case "expect-outcome": CmdExpectOutcome(rest, want: true);  break;
                case "expect-no-outcome": CmdExpectOutcome(rest, want: false); break;
                case "allow-flag-miss": CmdAllowFlagMiss(rest);       break;
                case "save":        CmdSave(rest);                    break;
                case "crash-report": CmdCrashReport(rest);            break;
                case "pause":       Report(_game.CliOpenPauseMenu() ? null : "could not open the pause menu",
                                           "opened the pause menu"); break;
                case "quit":        CmdQuit();                        break;
                default:            CliMode.Emit($"error: unknown command '{cmd}' (try `help`)"); break;
            }
        }
        catch (Exception ex)
        {
            CliMode.Emit($"error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// The preview box currently on screen, wherever it lives. Narration owns one and a conversation
    /// owns another, and <c>advance</c> has to drain whichever is up.
    ///
    /// <para>Reading only narration's made <c>advance</c> a no-op in Dialogue mode, so a dialogue
    /// script had to press <c>click continue</c> by hand and guess how many times — the reply options
    /// are themselves generated as a stack of previews, one press each. Same idiom everywhere now.</para>
    /// </summary>
    private (bool Active, string Title, string Text, bool Complete)? ActivePreview()
    {
        var dialogue = _game.CliDialogue?.Controller?.CliPreview();
        if (dialogue is { Active: true }) return dialogue;
        return _game.CliNarration?.CliPreview();
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
          world                     avatar vertex, biome, location, travel range, region
          world-regions [vertex]    the world's division into regions: sizes, seeds, borders,
                                    palette. With a vertex, just that vertex's region
          destinations              reachable vertices, by name
        Action
          click end-run             the death screen's END RUN button, back to the main menu
          save roundtrip            capture the run, serialise it, rebuild it and compare. Fails
                                    naming the first field that did not survive. The save test that
                                    fits a one-launch-per-script runner
          save dump | save read     print the LIVE run / what is ON DISK. To ASSERT on either, use
                                    `inspect save` / `inspect savefile` with expect-state — `expect`
                                    scans the rendered screen and never sees these
          save write | save erase   force a write / delete the save, bypassing autosave and death
          crash-report [text]       write a crash report and preserve log.txt under a name the next
                                    launch will not overwrite, then print that name. The reporter is
                                    otherwise unreachable from a script: it fires only when a phase
                                    fails, and --playground never makes the calls that can fail
          allow-flag-miss <flag>    declare that a narrowing flag is expected to match nothing, so
                                    its miss does not fail the run. For scripts that test an ABSENCE
          observe <name|none>       pin what an observation phase may look at, the way `goal` pins
                                    the goal draw. A two-phase script usually needs both
          expect-verb <verb-id>     assert which verb the last executed action carried. What a
                                    verb test must assert: `expect SUCCESS` matches ANY action's
                                    outcome banner and so passes on the wrong verb
          inspect [subject]         print the game state an outcome can change, by STABLE id:
                                    items / coins / where / party / wounds / skills / npcs / pois /
                                    routines / noetic / humors / world-regions, or all. What
                                    cli/outcome/ asserts on — the
                                    chip says the player was told, this says the world actually
                                    moved. `noetic` carries the phase budget and the acting body's
                                    tool proficiency, neither of which `expect` can reach
          expect-state <subj> <text>  assert `inspect <subj>` reports a line containing <text>.
                                    What cli/outcome/ asserts with: `expect` reads the SCREEN,
                                    this reads the world
          expect-no-state <subj> <text>  the negative form (an NPC gone, an item no longer held)
          expect-outcome <id>       assert an outcome of that id was applied this run (see
                                    --outcome-audit for the catalogue). `expect <chip>` proves
                                    what the player was TOLD; this proves what ran, which is the
                                    only way to assert the outcomes that show no chip
          expect-no-outcome <id>    the negative form, for asserting something did NOT happen
          click keyword <name|n>    click a narration keyword by name, or by position.
                                    The index form is for tests: a phase pinned with
                                    --observe-only knows what it is looking at but not
                                    which word the prose highlighted
          click action <n>          click a narration action by index
          click option <n>          pick a dialogue reply by index
          clock <days>              DEBUG: push the world clock forward, then heal any wound
                                    whose time has come (the clock otherwise only moves on
                                    travel and work, and wounds take 100-1000 days to close)
          click skill <name>        use a fighting skill by name (see `regions` in a fight)
          click fighter <name>      click a fighter's map cell — the target step for an attack
          click end-turn            end the active fighter's turn (the END TURN button)
          click engage              the travel encounter prompt's ENGAGE button
          click companion-death     the companion-death notice's CONTINUE — modal over every mode
          click menu <label>        press a main-menu button (New, Continue, …)
          click moon <name|n>       pick a world off the world-selection sky, by moon name or
                                    ordinal. Turns the sky towards it first, as the arrows would
          click sky                 press an empty patch of the world-selection sky, which releases
                                    the chosen moon without leaving the screen
          point moon <name|n>       hover a moon without choosing it. Hovering and choosing are
                                    separate states with separate looks; `click moon` drives both
          click world <confirm|cancel>  the world-selection screen's two buttons
          click arrow <direction>   press an on-screen camera arrow (left|right|up|down), the
                                    mouse's copy of the arrow keys
          click button              press the footer button (LEAVE/INTERRUPT/END/CONTINUE)
          click continue            confirm the dice overlay
          click cell <x> <y>        raw terminal cell click (escape hatch)
          choose <n>                answer the visible popup by index
          travel <vertex|name>      plan a route to a world vertex (bypasses 3D picking)
          travel-go                 commit the planned route and set out (the TRAVEL button)
          travel neighbour          plan a route to any bordering vertex (leaving, unnamed)
          travel back               plan a route to the last location entered that is not this one
          routines                  list the routines the planned destination offers
          routines <n>              replay routine n there (picks it and sets out)
          routines continue         press CONTINUE on the post-replay outcome box
          manage [tab]              open/close the protagonist screen; with a tab name
                                    (Anatomy, Inventory, Memory, Humors, …) open it there
          select [item name]        show a carried item's info panel; bare `select` lists them
          key <escape|…>            send a key
          scroll up|down [n]        scroll the shared history buffer
        Control
          strategy <succeed|fail-dice|fail-plausibility|auto>
                                    pin action outcomes (needs --debug)
          goal <verb-id|none>       pin the playground's goal choice to one verb (e.g. `goal tame`),
                                    set before the keyword click it should apply to
          fight-end <victory|death|runaway>
                                    force-resolve a fight to test its transition
          fight-deplete [enemies|companions|<fighter>]
                                    bleed a fighter's humors dry — the OTHER way to die in a fight,
                                    which ends it without touching hit points (default: enemies)
          fight-wound [enemies|companions|<fighter>]
                                    wound one fighter to death, which `fight-end` cannot do — the
                                    only way a script can kill a COMPANION (default: enemies)
          wound [protagonist|companions|<name>] [n]
                                    wound a party member OUTSIDE a fight, as a failed act's penalty
                                    does. No count means enough to kill (default: protagonist)
          starve [protagonist|companions|<name>]
                                    sour every humor queue to critical — the other lethal state a
                                    visit can arrive at (default: protagonist)
          cripple <mm-id> [protagonist|companions|<name>]
                                    High-wound every organ/region a modus mentis draws on, until it
                                    is BROKEN — effective level 0 or below, so it rolls nothing and
                                    every phase refuses it. `wound` draws from the catalogue at
                                    random, so this is the only way a script can break one NAMED
                                    modus mentis (default: protagonist)
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
            // A fight decided but held for the CONTINUE that shows its opening blow. On screen it is
            // an ordinary resolved action, so a script waiting on `wait mode Fighting` without
            // pressing CONTINUE would sit out its whole timeout with nothing to say why.
            if (n.HasDeferredFight) sb.Append(" fight-held=yes");
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
        // Modal over every mode, so a script that does not expect it reads as simply hung.
        if (_game.CliCompanionDeathShown)
            sb.Append(" companion-death=shown");

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

        if (_game.CliWorldSelectionButtons() is { } world)
        {
            var (count, selected, _) = _game.CliMoonState();
            CliMode.Emit(selected >= 0
                ? $"  moon {selected} selected: {Cathedral.Glyph.SkyMoons.Name(selected)} "
                  + $"(seed {Cathedral.Glyph.SkyMoons.WorldSeed(selected)})"
                : "  no moon selected yet");
            CliMode.Emit($"  click moon <name|ordinal>  ({count} moons, ordinals 0..{count - 1})");
            CliMode.Emit("  point moon <name|ordinal>  (hover it without choosing it)");
            CliMode.Emit("  click sky  (press empty sky — releases the chosen moon)");
            foreach (var (label, enabled, _, _) in world)
                CliMode.Emit($"  click world \"{label}\"{(enabled ? "" : "  (disabled)")}");
            CliMode.Emit("  click arrow left|right|up|down  (turn the sky)");
            return;
        }

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

        if (_game.CliCompanionDeathShown)
        {
            CliMode.Emit("  click companion-death  (CONTINUE — modal over every mode, nothing else answers)");
            any = true;
        }

        if (_game.CurrentMode == GameMode.EncounterPrompt)
        {
            CliMode.Emit("  click engage  (ENGAGE — the prompt's only button)");
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
        var regions = _game.CliWorld.Regions;
        CliMode.Emit($"avatar_vertex={v} biome=\"{biome.Name}\" location=\"{location?.Name ?? "-"}\" " +
                     $"region={regions?.RegionAt(v) ?? -1} landmass={regions?.LandmassAt(v) ?? -1} " +
                     $"mode={_game.CurrentMode}");
    }

    /// <summary>
    /// Prints the world's division into regions — the thing the developer R key colours in. Bare, it
    /// lists every region; with a vertex argument, only the region covering that vertex.
    /// </summary>
    private void CmdWorldRegions(string[] a)
    {
        var map = _game.CliWorld.Regions;
        if (map == null) { CliMode.Emit("error: no world generated"); return; }

        if (a.Length > 0)
        {
            if (!int.TryParse(a[0], out int vertex)) { CliMode.Emit("error: world-regions [vertex]"); return; }
            int id = map.RegionAt(vertex);
            if (id < 0) { CliMode.Emit($"vertex {vertex}: water (no region)"); return; }
            EmitRegion(map.Regions[id], vertex);
            return;
        }

        CliMode.Emit($"regions={map.Regions.Count} landmasses={map.LandmassCount} " +
                     $"overlay={(_game.CliWorld.RegionOverlayEnabled ? "on" : "off")}");
        foreach (var r in map.Regions) EmitRegion(r, null);
    }

    private void EmitRegion(Cathedral.Glyph.Microworld.WorldRegion r, int? queriedVertex)
    {
        var (biome, location, _) = _game.CliWorld.GetDetailedBiomeInfoAt(r.SeedVertex);
        string swatch = _game.CliWorld.Regions!.Palette[r.PaletteIndex].Name;
        string where = queriedVertex is int v ? $"vertex {v}: " : "  ";
        CliMode.Emit($"{where}region {r.Id}  landmass {r.LandmassId}  {r.CellCount} cells  " +
                     $"seed {r.SeedVertex} (\"{location?.Name ?? biome.Name}\")  " +
                     $"swatch \"{swatch}\"  borders [{string.Join(", ", r.Neighbours.OrderBy(x => x))}]");
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
        if (a.Length == 0) { CliMode.Emit("error: click <keyword|action|option|skill|fighter|end-turn|engage|menu|button|continue|cell> …"); return; }

        switch (a[0].ToLowerInvariant())
        {
            case "keyword":
            {
                if (a.Length < 2) { CliMode.Emit("error: click keyword <name|index>"); return; }
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
            case "end-run":
            {
                // The death screen's only button. Without a handle here a script can reach a death
                // but never leave it, so it cannot assert what the menu looks like afterwards.
                Report(_game.CliDeathEndRun() ? null : "not on the death screen", "ended the run");
                return;
            }

            case "end-turn":
            {
                var f = _game.CurrentMode == GameMode.Fighting ? _game.CliFight : null;
                if (f == null) { CliMode.Emit("error: not in a fight"); return; }
                Report(f.CliEndTurn(), "ended the turn");
                break;
            }
            case "companion-death":
            {
                // The notice's only button, and it is modal over every mode — a script that cannot
                // press it cannot get past a companion dying.
                Report(_game.CliDismissCompanionDeath(), "dismissed the companion-death notice");
                break;
            }
            case "engage":
            {
                // The encounter prompt's only button. Without it a staged travel encounter is a
                // dead end for a script: the prompt goes up and the fight behind it is unreachable.
                Report(_game.CliEngageEncounter(), "engaged the encounter");
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
            case "moon":
            {
                if (a.Length < 2) { CliMode.Emit("error: click moon <name|ordinal>"); return; }

                int ordinal = ResolveMoon(string.Join(' ', a[1..]).Trim('\"'));
                if (ordinal < 0) return;

                Report(_game.CliSelectMoon(ordinal),
                    $"selected moon {ordinal} ({Cathedral.Glyph.SkyMoons.Name(ordinal)}, "
                    + $"seed {Cathedral.Glyph.SkyMoons.WorldSeed(ordinal)})");
                break;
            }
            case "sky":
            {
                Report(_game.CliClickEmptySky(), "pressed empty sky (choice released)");
                break;
            }
            case "world":
            {
                if (a.Length < 2) { CliMode.Emit("error: click world <confirm|cancel>"); return; }
                var buttons = _game.CliWorldSelectionButtons();
                if (buttons == null) { CliMode.Emit("error: not on the world-selection screen"); return; }

                var match = buttons.FirstOrDefault(b => b.Label.Contains(a[1], StringComparison.OrdinalIgnoreCase));
                if (match.Label == null) { CliMode.Emit($"error: no world-selection button matching \"{a[1]}\""); return; }
                if (!match.Enabled)      { CliMode.Emit($"error: \"{match.Label}\" is disabled — no moon chosen yet"); return; }
                _game.CliHoverCell(match.X, match.Y);
                _game.CliClickCell(match.X, match.Y);
                CliMode.Emit($"ok: pressed \"{match.Label}\"");
                break;
            }
            case "arrow":
            {
                if (a.Length < 2) { CliMode.Emit("error: click arrow left|right|up|down"); return; }
                if (!Enum.TryParse<Cathedral.Game.CameraArrow>(a[1], ignoreCase: true, out var arrow)
                    || arrow == Cathedral.Game.CameraArrow.None)
                { CliMode.Emit($"error: unknown arrow '{a[1]}' (left|right|up|down)"); return; }

                var before = _game.CliCameraFacing();
                string? err = _game.CliPressCameraArrow(arrow);
                var after  = _game.CliCameraFacing();
                Report(err, $"pressed the {arrow} arrow (yaw {before.Yaw:F1}\u00b0 -> {after.Yaw:F1}\u00b0, "
                          + $"pitch {before.Pitch:F1}\u00b0 -> {after.Pitch:F1}\u00b0)");
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

    /// <summary>
    /// <c>point moon &lt;name|ordinal&gt;</c> — put the cursor on a moon without pressing.
    ///
    /// <para>Hovering and choosing are separate states on the world-selection screen, drawn
    /// differently and held at the same time. <c>click moon</c> drives both at once, so this is the
    /// only way a script can show that a choice survives the cursor wandering off it.</para>
    /// </summary>
    private void CmdPoint(string[] a)
    {
        if (a.Length < 2 || !a[0].Equals("moon", StringComparison.OrdinalIgnoreCase))
        { CliMode.Emit("error: point moon <name|ordinal>"); return; }

        int ordinal = ResolveMoon(string.Join(' ', a[1..]).Trim('"'));
        if (ordinal < 0) return;

        Report(_game.CliPointAtMoon(ordinal),
            $"pointed at moon {ordinal} ({Cathedral.Glyph.SkyMoons.Name(ordinal)})");
    }

    /// <summary>
    /// A moon ordinal from a name or a number, or -1 having already reported why not. Names are
    /// stable across processes (the sky is drawn from a constant), so a script may use either.
    /// </summary>
    private int ResolveMoon(string token)
    {
        if (int.TryParse(token, out int parsed)) return parsed;

        var (count, _, _) = _game.CliMoonState();
        for (int i = 0; i < count; i++)
            if (string.Equals(Cathedral.Glyph.SkyMoons.Name(i), token, StringComparison.OrdinalIgnoreCase))
                return i;

        CliMode.Emit($"error: no moon named \"{token}\"");
        return -1;
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

            // `travel back` plans a route to the last location the player was inside. A round trip is
            // the only way to reach routine replay — a routine replays on ARRIVAL — and a script
            // cannot name the vertex it started on, since that is whatever the seed put under the
            // avatar. Plans only, like any other named destination: follow with `travel-go`.
            // `travel neighbour` plans a route to any vertex bordering the avatar. The other half of
            // a round trip: a script that must leave a location and come back cannot name where it
            // is going either — `travel <name>` prefers the vertex under our feet, which would walk
            // straight back into the place it is trying to leave.
            if (want.Equals("neighbour", StringComparison.OrdinalIgnoreCase)
             || want.Equals("neighbor", StringComparison.OrdinalIgnoreCase))
            {
                int here = _game.CliAvatarVertex;
                int next = (_game.CliWorld.GetTravelGraph()?.GetConnectedNodes(here) ?? Enumerable.Empty<int>())
                    .FirstOrDefault(v => v != here
                                      && _game.CliWorld.IsVertexTraversable(v)
                                      && !_game.CliWorld.IsOutOfTravelRange(v), -1);
                if (next < 0) { CliMode.Emit("error: travel neighbour — no traversable neighbour in range"); return; }
                _game.CliClickVertex(next);
                CliMode.Emit($"ok: route planned to neighbouring vertex {next} — call `travel-go` to set out");
                return;
            }

            if (want.Equals("back", StringComparison.OrdinalIgnoreCase))
            {
                int last = _game.CliLastLocationVertex;
                if (last < 0) { CliMode.Emit("error: travel back — no location has been entered yet"); return; }
                if (last == _game.CliAvatarVertex)
                { CliMode.Emit($"error: travel back — already standing on vertex {last}"); return; }
                _game.CliClickVertex(last);
                CliMode.Emit($"ok: route planned back to vertex {last} — call `travel-go` to set out");
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
    /// The routine box: <c>routines</c> lists what the planned destination offers, <c>routines
    /// &lt;n&gt;</c> picks one and sets out (the replay runs on arrival), and <c>routines continue</c>
    /// presses CONTINUE on the outcome box afterwards, which applies the phase the routine ended on.
    ///
    /// <para>By index rather than by click because both the rows and the ROUTINES button are
    /// hit-tested against rendered boxes whose geometry moves with how many routines exist — the same
    /// reason <c>travel</c> injects a vertex instead of aiming at the sphere.</para>
    /// </summary>
    private void CmdRoutines(string[] a)
    {
        if (a.Length > 0 && a[0].Equals("continue", StringComparison.OrdinalIgnoreCase))
        {
            CliMode.Emit(_game.CliDismissRoutineOutcome()
                ? $"ok: routine outcome dismissed (mode={_game.CurrentMode})"
                : "error: no routine outcome box on screen");
            return;
        }

        if (_game.CurrentMode != GameMode.WorldView)
        { CliMode.Emit($"error: routines only works in WorldView (currently {_game.CurrentMode})"); return; }

        if (_game.CliRoutineEntries.Count == 0 && !_game.CliOpenRoutines())
        { CliMode.Emit("error: no travel plan — `travel <name>` first, then `routines`"); return; }

        var entries = _game.CliRoutineEntries;

        if (a.Length == 0)
        {
            CliMode.Emit($"routines: {entries.Count} for this destination");
            for (int i = 0; i < entries.Count; i++)
                CliMode.Emit($"  routines {i}  \"{entries[i].Name}\""
                           + (entries[i].Replayable ? "" : $"  (unreplayable: {entries[i].Reason})"));
            return;
        }

        if (!int.TryParse(a[0], out int index))
        { CliMode.Emit($"error: routines <n>|continue (got '{a[0]}')"); return; }

        if (!_game.CliSelectRoutine(index))
        {
            CliMode.Emit(index >= 0 && index < entries.Count
                ? $"error: routine {index} is not replayable: {entries[index].Reason}"
                : $"error: no routine {index} (offered 0..{entries.Count - 1})");
            return;
        }

        CliMode.Emit($"ok: replaying routine {index} on arrival (mode={_game.CurrentMode})");
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

    /// <summary>
    /// Pins what an observation phase may look at, the way <c>goal</c> pins the goal draw. Same
    /// switch as <c>--observe-only</c>; a command too, because a script with two phases usually needs
    /// to look at two different things.
    ///
    /// <para><c>cli/tame/success.cli</c> is why: it appeases a beast in one phase and tames it in the
    /// next, and the two are different objects only because the outcome of the first changes what the
    /// second may look at. A script that needs two phases to open on two things cannot say so with a
    /// launch flag, which is set once. <c>observe none</c> hands the choice back.</para>
    /// </summary>
    private static void CmdObserve(string[] a)
    {
        if (a.Length == 0) { CliMode.Emit($"observe={Config.Debug.ObserveOnly ?? "anything"}"); return; }

        string want = a[0];
        if (want.ToLowerInvariant() is "none" or "auto" or "off" or "anything")
        {
            Config.Debug.ObserveOnly = null;
            CliMode.Emit("ok: observe=anything");
            return;
        }

        Config.Debug.ObserveOnly = want;
        CliMode.Emit($"ok: observe={want}");
    }

    /// <summary>
    /// Pins the playground's goal choice to one verb, the way <c>strategy</c> pins the dice. Set it
    /// before the keyword click whose thinking phase should land on that goal; <c>goal none</c> (or
    /// <c>auto</c>) hands the choice back to the RNG. Same switch as <c>--goal-only</c> — a command
    /// too, because a script that appeases a beast and then tames it needs to change it mid-run.
    /// </summary>
    private static void CmdGoal(string[] a)
    {
        if (a.Length == 0) { CliMode.Emit($"goal={Config.Debug.GoalOnly ?? "auto"}"); return; }

        string want = a[0].ToLowerInvariant();
        if (want is "none" or "auto" or "off")
        {
            Config.Debug.GoalOnly = null;
            CliMode.Emit("ok: goal=auto");
            return;
        }

        if (Cathedral.Game.Scene.Verbs.VerbRegistry.Instance.Get(want) == null)
        {
            CliMode.Emit($"error: no verb '{want}'");
            return;
        }

        Config.Debug.GoalOnly = want;
        if (!PlaygroundMode.IsActive)
            CliMode.Emit("warning: --playground is not enabled, so the goal is chosen by the persona, not this");
        CliMode.Emit($"ok: goal={want}");
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

    private void CmdFightDeplete(string[] a)
    {
        var fight = _game.CliFight;
        if (fight == null) { CliMode.Emit("error: no fight in progress"); return; }
        string who = a.Length > 0 ? string.Join(' ', a).Trim('"') : "enemies";
        Report(fight.CliDepleteHumors(who), $"humors depleted: {who}");
    }

    private void CmdWound(string[] a)
    {
        string who   = a.Length > 0 ? a[0].Trim('"') : "protagonist";
        int    count = a.Length > 1 && int.TryParse(a[1], out int n) ? n : 0;
        Report(_game.CliWound(who, count), $"wounded: {who}{(count > 0 ? $" x{count}" : " (mortally)")}");
    }

    /// <summary>
    /// <c>cripple &lt;mm-id&gt; [who]</c> — wound every anatomy source a modus mentis draws on until it
    /// is broken. The only deterministic way a script can reach that state: <c>wound</c> draws from
    /// the catalogue at random, so landing a High-handicap wound on one named organ is a lottery.
    /// </summary>
    private void CmdCripple(string[] a)
    {
        if (a.Length < 1) { CliMode.Emit("error: cripple <modus-mentis-id> [protagonist|companions|<name>]"); return; }
        string mmId = a[0].Trim('"');
        string who  = a.Length > 1 ? string.Join(' ', a[1..]).Trim('"') : "protagonist";
        Report(_game.CliCripple(mmId, who), $"crippled: {mmId} on {who}");
    }

    private void CmdStarve(string[] a)
    {
        string who = a.Length > 0 ? a[0].Trim('"') : "protagonist";
        Report(_game.CliStarve(who), $"humors soured: {who}");
    }

    private void CmdFightWound(string[] a)
    {
        var fight = _game.CliFight;
        if (fight == null) { CliMode.Emit("error: no fight in progress"); return; }
        string who = a.Length > 0 ? string.Join(' ', a).Trim('"') : "enemies";
        Report(fight.CliWoundToDeath(who), $"wounded to death: {who}");
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

    /// <summary>
    /// Asserts which verb the last executed action actually carried.
    ///
    /// <para>The assertion a verb test needs, and the one <c>expect SUCCESS</c> is not. "SUCCESS" is
    /// the outcome banner of <i>any</i> action, so a script meant to test <c>break</c> that opened on
    /// a path instead, followed the path, and succeeded, passed. Sixty of them did. This reads the
    /// verb id off the action that ran, so a test can only pass on its own verb.</para>
    /// </summary>
    /// <summary>
    /// Declares that a narrowing flag is <i>expected</i> to match nothing, so its miss does not fail
    /// the run.
    ///
    /// <para>Needed because some scripts test an absence. <c>cli/gather/beast_barred.cli</c> checks
    /// that a beast is not offered <c>gather</c> at all — so <c>--goal-only gather</c> finding no such
    /// goal is the assertion passing, not the test misfiring. Without this the strictness that catches
    /// a mis-aimed test would make an absence test impossible to write.</para>
    /// </summary>
    private void CmdAllowFlagMiss(string[] a)
    {
        if (a.Length == 0) { CliMode.Emit("error: allow-flag-miss <flag>"); return; }
        _allowedFlagMisses.Add(a[0]);
        CliMode.Emit($"ok: a miss of {a[0]} is expected here");
    }

    /// <summary>Flags whose misses this script has declared expected — see <see cref="CmdAllowFlagMiss"/>.</summary>
    private readonly HashSet<string> _allowedFlagMisses = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The save system, in five subcommands.
    ///
    /// <para><c>roundtrip</c> is the one that earns its keep. The suite runs one script per launch, so
    /// a real save-quit-relaunch test cannot be written in it; this does the whole cycle in memory —
    /// capture, serialise, rebuild, re-capture, compare — and fails naming the first field that did not
    /// survive. Append it to any script after doing something interesting.</para>
    ///
    /// <para><c>dump</c> summarises the LIVE run, <c>read</c> summarises what is ON DISK. They differ
    /// exactly when the autosave has not fired since the last change, which is what makes them useful
    /// as two separate assertions.</para>
    /// </summary>
    /// <summary>
    /// Forces a crash report, so the reporter itself is testable.
    ///
    /// <para>Every real trigger is a phase that failed, and a phase fails on an exception a script
    /// cannot arrange — the failure this was built for was a socket fault that struck twice in 228
    /// requests. Worse, <c>--playground</c> replaces every LLM call before it is made, so the whole
    /// suite runs without ever making a call that could fail. Without this command the crash reporter
    /// would ship having only ever been exercised by the bug it exists to catch.</para>
    ///
    /// <para><b>It asserts on its own behalf</b>, the way <c>save roundtrip</c> does, because the
    /// thing worth checking is not on screen: <c>expect</c> scans the rendered terminal and a crash
    /// report is written to a file. So this reads the preserved copy back off disk and fails naming
    /// the section that is missing — which also proves the copy really contains the diagnosis, rather
    /// than proving a file of that name exists.</para>
    /// </summary>
    private void CmdCrashReport(string[] a)
    {
        string what = a.Length > 0 ? string.Join(' ', a) : "a forced test report";

        // A thrown-and-caught exception, so the report exercises the real stack-trace and
        // inner-exception formatting rather than the "(none)" branch.
        Exception captured;
        try { throw new InvalidOperationException($"Forced by the CLI: {what}"); }
        catch (Exception ex) { captured = ex; }

        string? file = CrashReport.Capture(what, captured);
        if (file == null)
        {
            CliMode.Emit("FAIL: crash report preserved no log (is log.txt open and the folder writable?)");
            CliMode.HasFailedAssertion = true;
            return;
        }

        string contents;
        try
        {
            // Same folder as log.txt, which is where PreserveCopy puts it.
            var path = System.IO.Path.Combine(Environment.CurrentDirectory, file);
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);
            contents = reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            CliMode.Emit($"FAIL: crash report '{file}' could not be read back — {ex.GetType().Name}: {ex.Message}");
            CliMode.HasFailedAssertion = true;
            return;
        }

        // The sections a reader needs, and the ones a refactor would drop in silence. "LLM server" is
        // contributed by LlamaServerManager rather than by CrashReport, so its absence would mean the
        // provider mechanism itself has stopped working.
        string[] required = { "CRASH REPORT", what, "── Exception ──", "InvalidOperationException",
                              "Stack trace:", "── Environment ──", "Wine:", "── LLM server ──",
                              "Health, pooled", "Health, fresh conn", "Last" };

        var missing = required.Where(section => !contents.Contains(section)).ToList();
        if (missing.Count > 0)
        {
            CliMode.Emit($"FAIL: crash report '{file}' is missing: {string.Join(", ", missing)}");
            CliMode.HasFailedAssertion = true;
            return;
        }

        CliMode.Emit($"ok: crash report preserved as {file} ({contents.Length} bytes, all sections present)");
    }

    private void CmdSave(string[] a)
    {
        string sub = a.Length > 0 ? a[0].ToLowerInvariant() : "dump";
        switch (sub)
        {
            case "roundtrip":
                string? divergence = _game.CliSaveRoundTrip();
                if (divergence == null)
                {
                    CliMode.Emit("ok: save round-trip survived");
                }
                else
                {
                    // A real assertion, not a report: losing state across a save is a test failure and
                    // must set the run's exit code, the way expect/expect-state do.
                    CliMode.Emit($"FAIL: save round-trip lost state — {divergence}");
                    CliMode.HasFailedAssertion = true;
                }
                break;

            case "dump":
                CliMode.EmitBlock(string.Join("\n", _game.CliSaveDump()));
                break;

            case "read":
                CliMode.EmitBlock(string.Join("\n", _game.CliSaveRead()));
                break;

            case "write":
                Report(_game.CliSaveWrite() ? null : "the save could not be written", "save written");
                break;

            case "erase":
                _game.CliSaveErase();
                CliMode.Emit("ok: save erased");
                break;

            default:
                CliMode.Emit($"error: save <roundtrip|dump|read|write|erase> (got '{sub}')");
                break;
        }
    }

    /// <summary>
    /// Asserts that an outcome of the given id has (or has not) been applied this run.
    ///
    /// <para>The counterpart to <c>expect-verb</c>, one level down. <c>expect &lt;chip&gt;</c> proves
    /// what the player was <i>told</i>; this proves what actually <i>ran</i>. That distinction is
    /// what makes the four chip-less outcomes testable at all — <c>state_capture</c> and the phase
    /// transitions show nothing on screen — and it lets a test name an outcome by its stable id
    /// rather than by wording that is free to change.</para>
    ///
    /// <para>Reads <see cref="Outcome.Applied"/>, appended by <c>Outcome.ApplyTo</c>, which is the
    /// only way an outcome can be carried out.</para>
    /// </summary>
    /// <summary>
    /// Prints the game state an outcome can change: carried items, coins, where and when, the party,
    /// wounds, skills, the scene's NPCs and the area's points of interest.
    ///
    /// <para>What the <c>cli/outcome/</c> range is built on. A verb test asserts the chip — that the
    /// player was told something happened; an outcome test inspects before and after and asserts the
    /// world really moved. Everything is named by stable id so an assertion survives a content
    /// rewrite.</para>
    /// </summary>
    private void CmdInspect(string[] a)
    {
        string subject = a.Length == 0 ? "all" : a[0].ToLowerInvariant();

        // Some facts outlive the narration that made them — routines are finalised as a session
        // ENDS, so they can only be read once the narrative controller is gone.
        var lines = _game.CliInspectGlobal(subject);
        if (lines == null)
        {
            var n = _game.CliNarration;
            if (n == null) { CliMode.Emit("error: not in narration"); return; }
            lines = n.CliInspect(subject);
        }
        if (lines.Count == 0) { CliMode.Emit($"inspect {subject}: (nothing)"); return; }
        foreach (var line in lines) CliMode.Emit(line);
    }

    /// <summary>
    /// Asserts that <c>inspect &lt;subject&gt;</c> does (or does not) report a line containing the
    /// given text. The assertion the <c>cli/outcome/</c> range is built on.
    ///
    /// <para>Separate from <c>expect</c> because they read different things: <c>expect</c> scans the
    /// rendered terminal, which is what the player sees, while this reads the world itself. An
    /// outcome test needs the second — a chip proves the player was told, not that anything
    /// changed.</para>
    /// </summary>
    private void CmdExpectState(string[] a, bool want)
    {
        if (a.Length < 2)
        {
            CliMode.Emit($"error: expect{(want ? "" : "-no")}-state <subject> <text>");
            return;
        }

        string subject = a[0].ToLowerInvariant();
        string needle  = string.Join(' ', a.Skip(1));

        // Same fall-through as CmdInspect: a subject that outlives narration (routines) must still
        // be assertable once the session that produced it has ended.
        var lines = _game.CliInspectGlobal(subject);
        if (lines == null)
        {
            var n = _game.CliNarration;
            if (n == null) { CliMode.Emit("error: not in narration"); return; }
            lines = n.CliInspect(subject);
        }
        bool   found   = lines.Any(l => l.Contains(needle, StringComparison.OrdinalIgnoreCase));

        if (found == want)
        {
            CliMode.Emit(want ? $"PASS: {subject} has \"{needle}\""
                              : $"PASS: {subject} has no \"{needle}\"");
            return;
        }

        CliMode.Emit(want
            ? $"FAIL: {subject} has no \"{needle}\" — saw: "
              + (lines.Count == 0 ? "(nothing)" : string.Join(" | ", lines.Take(8)))
            : $"FAIL: {subject} still has \"{needle}\"");
        CliMode.HasFailedAssertion = true;
    }

    private void CmdExpectOutcome(string[] a, bool want)
    {
        if (a.Length == 0)
        {
            CliMode.Emit($"error: expect{(want ? "" : "-no")}-outcome <outcome-id>");
            return;
        }

        string id      = a[0];
        bool   applied = Narrative.Outcome.Applied.Contains(id, StringComparer.OrdinalIgnoreCase);

        if (applied == want)
        {
            CliMode.Emit(want ? $"PASS: outcome '{id}' was applied"
                              : $"PASS: outcome '{id}' was not applied");
            return;
        }

        CliMode.Emit(want
            ? $"FAIL: expected outcome '{id}' — applied so far: "
              + (Narrative.Outcome.Applied.Count == 0 ? "(none)"
                 : string.Join(", ", Narrative.Outcome.Applied.Distinct()))
            : $"FAIL: outcome '{id}' was applied and should not have been");
        CliMode.HasFailedAssertion = true;
    }

    private void CmdExpectVerb(string[] a)
    {
        if (a.Length == 0) { CliMode.Emit("error: expect-verb <verb-id>"); return; }
        var n = _game.CliNarration;
        if (n == null) { CliMode.Emit("error: not in narration"); return; }

        var actual = n.CliLastExecutedVerbId();
        if (actual == null)
        {
            CliMode.Emit($"FAIL: expected verb '{a[0]}' — no action has been executed yet");
            CliMode.HasFailedAssertion = true;
            return;
        }
        if (string.Equals(actual, a[0], StringComparison.OrdinalIgnoreCase))
        {
            CliMode.Emit($"PASS: executed verb was '{actual}'");
            return;
        }
        CliMode.Emit($"FAIL: expected verb '{a[0]}' but '{actual}' was executed");
        CliMode.HasFailedAssertion = true;
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
        // A narrowing flag that narrowed nothing fails the run. --start-area, --observe-only and
        // --goal-only all fall back silently when they match nothing (open where the factory did,
        // offer the whole scene, draw from every goal), which is right for a person poking at the
        // game and disastrous for a test: the script goes on to exercise something other than what it
        // was written for, and reports whatever THAT did. See DebugFlagAudit — this is not
        // hypothetical, it is how 59 verb tests came to pass without ever running their verb.
        foreach (var miss in Cathedral.Game.DebugFlagAudit.Misses)
        {
            // A script may declare a miss expected — see `allow-flag-miss`. Some tests are about an
            // absence, and for those the miss IS the result.
            if (_allowedFlagMisses.Any(f => miss.StartsWith(f, StringComparison.OrdinalIgnoreCase)))
            {
                CliMode.Emit($"ok (expected): {miss}");
                continue;
            }
            CliMode.Emit($"FAIL: {miss} — the script did not test what it names");
            CliMode.HasFailedAssertion = true;
        }

        CliMode.Emit(CliMode.HasFailedAssertion ? "quitting (assertions FAILED)" : "quitting (all assertions passed)");
        _game.CliRequestClose();
    }

    private static void Report(string? error, string okMessage)
        => CliMode.Emit(error == null ? $"ok: {okMessage}" : $"error: {error}");
}
