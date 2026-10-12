using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Cli;
using Cathedral.Glyph;
using OpenTK.Mathematics;

namespace Cathedral.Game.Record;

/// <summary>
/// The <c>--record</c> half of the CLI driver. Every command passes through <see cref="Intercept"/>
/// first, which sorts it into one of four kinds:
/// <list type="bullet">
/// <item><b>Pointer</b> (<c>click</c>, <c>choose</c>, <c>travel</c>, <c>travel-go</c>, <c>point</c>,
/// <c>scroll</c>): the target is located as a screen pixel — through the same hit-test the game will
/// answer the click with — and the drawn cursor travels there and presses. The game is told through
/// <see cref="GlyphSphereCore.InjectPointerMove"/> and friends, which is the path a real mouse takes.
/// No pixel, no click: the command is refused.</item>
/// <item><b>Keyboard</b> (<c>key</c>, <c>pause</c>): performed by the CLI as usual, and logged.</item>
/// <item><b>Recording</b> (<c>mark</c>, <c>clip</c>, <c>note</c>, <c>hold</c>, <c>cursor</c>): direct
/// the film, not the game.</item>
/// <item><b>Forbidden</b>: everything that reaches past the screen — forced dice, wounds, the clock,
/// opening a screen no button opens. Refused, and the run is marked failed.</item>
/// </list>
/// Everything else (reading the screen, waiting, asserting, quitting) passes through untouched.
/// </summary>
public sealed class RecordGate
{
    private readonly LocationTravelGameController _game;
    private readonly CliDriver _driver;
    private readonly RecordPointer _pointer;
    private GlyphSphereCore Core => _game.CliCore;

    private double _holdUntil;
    private Vector2 _lastInjected = new(-1, -1);
    private GameMode? _lastMode;
    private bool _thinking;
    private int _gesture;   // gestures started, for a deterministic nudge inside the target

    private static readonly HashSet<string> Forbidden = new(StringComparer.OrdinalIgnoreCase)
    {
        "strategy", "goal", "observe", "fight-end", "fight-deplete", "fight-wound", "wound", "cripple",
        "starve", "clock", "save", "crash-report", "manage", "select", "routines",
    };

    private static readonly HashSet<string> Pointer = new(StringComparer.OrdinalIgnoreCase)
    {
        "click", "choose", "travel", "travel-go", "point", "scroll",
    };

    public RecordGate(LocationTravelGameController game, CliDriver driver, RecordPointer pointer)
    {
        _game = game;
        _driver = driver;
        _pointer = pointer;
        Cathedral.Audio.AmbianceEngine.GameEventFired += evt => RecordMode.Log("sfx", new() { ["event"] = evt.ToString() });
    }

    /// <summary>True while a gesture or a <c>hold</c> is playing out: the driver runs nothing else meanwhile.</summary>
    public bool Busy => _pointer.Busy || RecordMode.Now < _holdUntil || _scrollTarget != null;

    /// <summary>A `choose` waiting for its row to scroll into view: the choice, its command, wheel notches so far.</summary>
    private (int Index, string Line, int Steps)? _scrollTarget;
    private double _nextWheel;

    /// <summary>
    /// Called on every CLI tick. Moves the gesture on, tells the game where the pointer is, and logs
    /// mode changes. Returns true while the driver should wait.
    /// </summary>
    public bool Tick()
    {
        double now = RecordMode.Now;

        if (_game.CurrentMode != _lastMode)
        {
            RecordMode.Log("mode", new() { ["from"] = _lastMode?.ToString(), ["to"] = _game.CurrentMode.ToString() });
            _lastMode = _game.CurrentMode;
        }

        // Spans spent waiting on the model, for the cutter to fast-forward.
        bool thinking = _game.CliModelBusy();
        if (thinking != _thinking)
        {
            RecordMode.Log(thinking ? "model-busy" : "model-idle");
            _thinking = thinking;
        }

        if (_pointer.Busy)
        {
            var p = _pointer.Advance(now);
            if ((p - _lastInjected).LengthSquared > 0.25f) Inject(p);
        }
        else if (_scrollTarget is { } st && now >= _nextWheel)
        {
            // One notch per tick, down first and then back up, until the row is drawn.
            if (_game.CliNarration?.CliPopupChoicePixel(st.Index) is { } row)
            {
                _scrollTarget = null;
                var labels = _game.CliNarration?.CliPopup()?.Labels;
                Press(row, labels != null && st.Index < labels.Count ? labels[st.Index] : $"choice {st.Index}", st.Line);
            }
            else if (st.Steps >= 40 || _game.CliNarration?.CliPopup() == null)
            {
                _scrollTarget = null;
                Refuse($"could not scroll popup choice {st.Index} into view");
            }
            else
            {
                Core.InjectWheel(st.Steps < 20 ? -1f : 1f);
                RecordMode.Log("wheel", new() { ["delta"] = st.Steps < 20 ? -1 : 1 });
                _scrollTarget = (st.Index, st.Line, st.Steps + 1);
                _nextWheel = now + 0.18;
            }
        }
        return Busy;
    }

    private void Inject(Vector2 p)
    {
        _lastInjected = p;
        Core.InjectPointerMove(p);
    }

    /// <summary>
    /// Handles a command in record mode. Returns true when it was handled here (performed, started as a
    /// gesture, or refused) and false when the CLI should run it as usual.
    /// </summary>
    public bool Intercept(string cmd, string[] rest, string line)
    {
        RecordMode.Log("cmd", new() { ["line"] = line });

        if (Forbidden.Contains(cmd))
        {
            Refuse($"`{cmd}` is not something a player can do — refused in --record. "
                 + (cmd is "manage" or "select" or "routines"
                    ? "Reach that screen the way a player does: `pause`, `click menu Protagonist`, `click tab inventory` (`elements` lists what is clickable)."
                    : "Set the situation up with a starting flag instead (--start-at, --location-type, --grant-item…)."));
            return true;
        }

        switch (cmd.ToLowerInvariant())
        {
            case "mark":
                RecordMode.Log("mark", new() { ["label"] = string.Join(' ', rest) });
                CliMode.Emit($"ok: mark \"{string.Join(' ', rest)}\" at {Secs(RecordMode.Now)}");
                return true;
            case "note":
                RecordMode.Log("note", new() { ["text"] = string.Join(' ', rest) });
                CliMode.Emit("ok: note");
                return true;
            case "clip":
            {
                bool end = rest.Length > 0 && rest[0].Equals("end", StringComparison.OrdinalIgnoreCase);
                string name = string.Join(' ', end ? rest.Skip(1) : rest);
                RecordMode.Log(end ? "clip-end" : "clip-begin", new() { ["name"] = name });
                CliMode.Emit($"ok: clip {(end ? "end" : "begin")} \"{name}\" at {Secs(RecordMode.Now)}");
                return true;
            }
            case "hold":
            {
                double secs = rest.Length > 0 && double.TryParse(rest[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var s) ? s : 1.5;
                _holdUntil = RecordMode.Now + secs;
                CliMode.Emit($"ok: holding the shot {Secs(secs)}");
                return true;
            }
            case "cursor":
                return CmdCursor(rest);
            case "dice":
            {
                // A settled roll, as a player reads it: the dice, the verdict, and which humor would
                // change which die. What a script consults before `click humor` / `click die`.
                var d = _game.CliActiveDice;
                if (d == null) { CliMode.Emit("no dice roll on screen"); return true; }
                foreach (var l in d.CliDescribe()) CliMode.Emit(l);
                return true;
            }
            case "elements":
            {
                // The named controls on screen — what `click element` (and `click tab`/`back`) accept.
                var ids = _game.CliElementIds();
                CliMode.Emit(ids.Count == 0 ? "no named controls on screen" : "elements: " + string.Join(", ", ids));
                return true;
            }
            case "key":
            case "pause":
                RecordMode.Log("key", new() { ["key"] = cmd == "pause" ? "escape" : rest.FirstOrDefault() });
                return false;   // the keyboard needs no aiming; the CLI performs it
        }

        if (!Pointer.Contains(cmd)) return false;

        string? error = Plan(cmd.ToLowerInvariant(), rest, line);
        if (error != null) Refuse(error);
        return true;
    }

    /// <summary>The CONTINUE press <c>advance</c> makes between settles, aimed like any other click.</summary>
    public void PressContinue() => Intercept("click", new[] { "continue" }, "click continue (advance)");

    /// <summary>Seconds as the timeline writes them, whatever the machine's locale.</summary>
    private static string Secs(double s) => s.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "s";

    private static void Refuse(string why)
    {
        CliMode.HasFailedAssertion = true;
        CliMode.Emit($"error: {why}");
        RecordMode.Log("refused", new() { ["why"] = why });
    }

    // ── Gestures ──────────────────────────────────────────────────────────────

    /// <summary>Locates the command's target and starts the gesture. Returns why not, or null.</summary>
    private string? Plan(string cmd, string[] a, string line)
    {
        switch (cmd)
        {
            case "click":
            {
                if (a.Length == 0) return "click <target> …";
                double hold = 0;
                if (a[0].Equals("arrow", StringComparison.OrdinalIgnoreCase) && a.Length > 2)
                    double.TryParse(a[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out hold);
                var (pixel, label, err) = LocateClick(a);
                if (err != null) return err;
                Press(pixel, label, line, hold > 0 ? hold : (a[0].Equals("arrow", StringComparison.OrdinalIgnoreCase) ? 0.6 : 0));
                return null;
            }
            case "choose":
            {
                if (a.Length < 1 || !int.TryParse(a[0], out int idx)) return "choose <n>";
                var labels0 = _game.CliNarration?.CliPopup()?.Labels;
                if (labels0 == null) return "no popup on screen";
                if (idx < 0 || idx >= labels0.Count) return $"no popup choice {idx} (0..{labels0.Count - 1})";
                var px = _game.CliNarration?.CliPopupChoicePixel(idx);
                if (px == null)
                {
                    // Out of view in a long list: scroll to it with the wheel, as a player would.
                    if (Core.PopupTerminal?.GetScreenBounds(Core.ClientSize) is not { } pb) return "the popup has no bounds";
                    _scrollTarget = (idx, line, 0);
                    Move(new Vector2((pb.left + pb.right) / 2, (pb.top + pb.bottom) / 2), "popup", line);
                    return null;
                }
                var labels = _game.CliNarration?.CliPopup()?.Labels;
                Press(px.Value, labels != null && idx < labels.Count ? labels[idx] : $"choice {idx}", line);
                return null;
            }
            case "travel":
            {
                var (vertex, err) = _driver.ResolveTravel(a);
                if (err != null) return err;
                if (!Core.TryGetClickableVertexPixel(vertex, out var px))
                    return $"vertex {vertex} cannot be clicked from this camera angle — turn the globe first (`click arrow left 1.5`)";
                var (biome, location, _) = _game.CliWorld.GetDetailedBiomeInfoAt(vertex);
                Press(px, location?.Name ?? biome.Name, line);
                return null;
            }
            case "travel-go":
            {
                var cell = _game.CliFindElementCell("travel-button");
                if (cell == null) return "no TRAVEL button on screen — plan a route with `travel <name>` first";
                Press(CellPixel(cell.Value.X, cell.Value.Y), "TRAVEL", line);
                return null;
            }
            case "point":
            {
                if (a.Length < 2 || !a[0].Equals("moon", StringComparison.OrdinalIgnoreCase)) return "point moon <name|ordinal>";
                int ordinal = ResolveMoon(string.Join(' ', a[1..]).Trim('"'));
                if (ordinal < 0) return $"no moon named \"{string.Join(' ', a[1..])}\"";
                if (!Core.TryGetMoonScreenPosition(ordinal, out var px))
                    return $"moon {SkyMoons.Name(ordinal)} is not in view — turn the sky first (`click arrow …`)";
                Move(px, SkyMoons.Name(ordinal), line);
                return null;
            }
            case "scroll":
            {
                string dir = a.Length > 0 ? a[0].ToLowerInvariant() : "down";
                int n = a.Length > 1 && int.TryParse(a[1], out int parsed) ? Math.Max(1, parsed) : 3;
                float delta = dir == "up" ? 1f : -1f;
                Move(CellPixel(Config.Terminal.MainWidth / 2, Config.Terminal.MainHeight / 3), $"scroll {dir}", line, () =>
                {
                    for (int i = 0; i < n; i++) Core.InjectWheel(delta);
                    RecordMode.Log("wheel", new() { ["delta"] = delta * n });
                });
                return null;
            }
        }
        return $"`{cmd}` has no pointer route";
    }

    private void Press(Vector2 pixel, string label, string line, double holdSeconds = 0)
    {
        _gesture++;
        RecordMode.Log("move", new() { ["x"] = Math.Round(pixel.X), ["y"] = Math.Round(pixel.Y), ["label"] = label });
        _pointer.Start(pixel, RecordMode.Now, press: true, holdSeconds: holdSeconds,
            onPress: () =>
            {
                // Logged BEFORE the press is delivered: a press can run a long way synchronously (New
                // generates a whole world), and the click happened when the button went down.
                RecordMode.Log("click", new() { ["x"] = Math.Round(pixel.X), ["y"] = Math.Round(pixel.Y), ["label"] = label, ["cmd"] = line });
                Inject(pixel);
                Core.InjectPointerDown();
            },
            onRelease: () => Core.InjectPointerUp(),
            onDone: () => CliMode.Emit($"ok: clicked \"{label}\" at ({pixel.X:F0},{pixel.Y:F0})"));
    }

    private void Move(Vector2 pixel, string label, string line, Action? then = null)
    {
        _gesture++;
        RecordMode.Log("move", new() { ["x"] = Math.Round(pixel.X), ["y"] = Math.Round(pixel.Y), ["label"] = label });
        _pointer.Start(pixel, RecordMode.Now, press: false, onDone: () =>
        {
            Inject(pixel);
            then?.Invoke();
            CliMode.Emit($"ok: pointer on \"{label}\" at ({pixel.X:F0},{pixel.Y:F0})");
        });
    }

    private bool CmdCursor(string[] a)
    {
        string what = a.Length > 0 ? a[0].ToLowerInvariant() : "";
        switch (what)
        {
            case "hide": _pointer.Visible = false; break;
            case "show": _pointer.Visible = true; break;
            case "rest":
                // Out of the way of the text: the lower right, where nothing in the game is drawn.
                Move(new Vector2(Core.ClientSize.X * 0.93f, Core.ClientSize.Y * 0.9f), "rest", "cursor rest");
                return true;
            default:
                CliMode.Emit("error: cursor hide|show|rest");
                return true;
        }
        RecordMode.Log("cursor", new() { ["visible"] = _pointer.Visible });
        CliMode.Emit($"ok: cursor {what}");
        return true;
    }

    // ── Locating ──────────────────────────────────────────────────────────────

    /// <summary>The pixel a <c>click …</c> aims at, a label for the timeline, or why there is none.</summary>
    private (Vector2 Pixel, string Label, string? Error) LocateClick(string[] a)
    {
        string what = a[0].ToLowerInvariant();
        string arg = string.Join(' ', a.Skip(1)).Trim('"');
        var narration = _game.CliNarration;
        var dialogue = _game.CliDialogue?.Controller;
        var fight = _game.CurrentMode == GameMode.Fighting ? _game.CliFight : null;

        (Vector2, string, string?) Fail(string why) => (default, "", why);
        (Vector2, string, string?) At((int X, int Y) c, string label) => (CellPixel(c.X, c.Y), label, null);
        (Vector2, string, string?) Span((int X, int Y, int W) s, string label) => (SpanPixel(s), label, null);

        switch (what)
        {
            case "keyword":
            {
                var words = narration?.CliKeywords();
                string word = int.TryParse(arg, out int ki) && words != null && ki >= 0 && ki < words.Count ? words[ki] : arg;
                return narration?.CliKeywordSpan(arg) is { } k ? Span(k, word) : Fail($"no clickable keyword '{arg}' on screen");
            }

            case "action":
                if (!int.TryParse(arg, out int ai)) return Fail("click action <n>");
                return narration?.CliActionSpan(ai) is { } act
                    ? Span(act, narration.CliActions().FirstOrDefault(x => x.Index == ai).Text ?? $"action {ai}")
                    : Fail($"no action {ai} on screen");

            case "option":
                if (!int.TryParse(arg, out int oi)) return Fail("click option <n>");
                return dialogue?.CliOptionCell(oi) is { } oc
                    ? At(oc, dialogue.CliOptions().FirstOrDefault(o => o.Index == oi).Text ?? $"option {oi}")
                    : Fail($"no reply {oi} on screen");

            case "skill":
            {
                if (fight == null) return Fail("not in a fight");
                string name = arg.EndsWith("(learn)", StringComparison.OrdinalIgnoreCase) ? arg[..^"(learn)".Length].TrimEnd() : arg;
                var (right, bottom) = fight.CliActionMenuBounds();
                return FindText(name, 0, 0, right, bottom) is { } sc ? At(sc, name) : Fail($"no skill \"{name}\" in the action menu");
            }

            case "fighter":
                return fight?.CliFighterCell(arg) is { } fc ? At(fc, arg) : Fail($"no fighter matching \"{arg}\"");

            case "end-turn":
                return fight != null ? At(fight.CliEndTurnCell(), "END TURN") : Fail("not in a fight");

            case "engage":
                return _game.CliFindElementCell("encounter-engage") is { } ec ? At(ec, "ENGAGE") : Fail("no ENGAGE button on screen");

            case "end-run":
                return _game.CliFindElementCell("death-end-run") is { } dc ? At(dc, "END RUN") : Fail("not on the death screen");

            case "companion-death":
                return _game.CliCompanionDeathCell is { } cd ? At(cd, "CONTINUE") : Fail("no companion-death notice on screen");

            case "button":
                if (dialogue?.CliExitSpan() is { } de) return Span(de, "footer button");
                if (narration?.CliExitButton() is { Present: true } nb) return Span((nb.X, nb.Y, nb.Width), "footer button");
                return Fail("no footer button on screen");

            case "continue":
            {
                if (_game.CliCreationContinue() is { } cc)
                {
                    // Aim at the middle of the button the creation screen's own hover rule outlines.
                    var id = _game.CliElementIdAt(cc.X, cc.Y);
                    return At(id != null && _game.CliFindElementCell(id) is { } mid ? mid : cc, "Continue");
                }
                if (fight != null)
                    return fight.CliDiceContinueSpan() is { } fd ? Span(fd, "Continue") : Fail("no settled dice box");
                if (dialogue != null)
                    return dialogue.CliContinueSpan() is { } dcs ? Span(dcs, "CONTINUE") : Fail("no CONTINUE in the conversation");
                if (narration == null) return Fail("not in narration");
                var pv = narration.CliPreviewContinue();
                if (pv.Present) return Span((pv.X, pv.Y, pv.Width), "CONTINUE");
                var dice = narration.CliDiceContinue();
                return dice.Present ? Span((dice.X, dice.Y, dice.Width), "Continue") : Fail("no continue button on screen");
            }

            case "menu":
            {
                var menu = _game.CliMenuButtons();
                if (menu == null) return Fail("not on the main menu");
                int i = menu.ToList().FindIndex(b => b.Label.Contains(arg, StringComparison.OrdinalIgnoreCase));
                if (i < 0) return Fail($"no menu button matching \"{arg}\"");
                if (!menu[i].Enabled) return Fail($"menu button \"{menu[i].Label}\" is disabled");
                return _game.CliFindElementCell($"menu:{i}") is { } mc ? At(mc, menu[i].Label) : At((menu[i].X + 4, menu[i].Y), menu[i].Label);
            }

            case "world":
            {
                string id = arg.StartsWith("conf", StringComparison.OrdinalIgnoreCase) ? "world-confirm" : "world-cancel";
                return _game.CliFindElementCell(id) is { } wc ? At(wc, id) : Fail($"no {id} button on screen (no moon chosen yet?)");
            }

            case "moon":
            {
                int ordinal = ResolveMoon(arg);
                if (ordinal < 0) return Fail($"no moon named \"{arg}\"");
                return Core.TryGetMoonScreenPosition(ordinal, out var mp)
                    ? (mp, SkyMoons.Name(ordinal), null)
                    : Fail($"moon {SkyMoons.Name(ordinal)} is not in view — turn the sky first (`click arrow …`)");
            }

            case "sky":
                return Core.TryFindEmptySkyPoint(out var sp) ? (sp, "empty sky", null) : Fail("no empty patch of sky in view");

            case "arrow":
            {
                if (!Enum.TryParse<CameraArrow>(a.Length > 1 ? a[1] : "", ignoreCase: true, out var arrow) || arrow == CameraArrow.None)
                    return Fail("click arrow left|right|up|down [seconds]");
                return _game.CliCameraArrowCell(arrow) is { } ac ? At(ac, $"{arrow} arrow") : Fail("the camera arrows are not on screen");
            }

            case "element":
            case "tab":
            case "back":
            {
                // A control named by its screen's own rule: menus, settings, the protagonist screen's
                // tabs, trade and work. `elements` lists them.
                string want = what == "tab" ? $"tab:{arg.ToLowerInvariant()}" : what == "back" ? "back" : arg;
                var ids = _game.CliElementIds();
                string? id = ids.FirstOrDefault(i => i.Equals(want, StringComparison.OrdinalIgnoreCase))
                          ?? ids.FirstOrDefault(i => i.Contains(want, StringComparison.OrdinalIgnoreCase));
                if (id == null) return Fail($"no control \"{want}\" on screen (elements: {string.Join(", ", ids)})");
                return _game.CliFindElementCell(id) is { } ec2 ? At(ec2, id) : Fail($"control \"{id}\" has no cell");
            }

            case "humor":
            {
                // Select an organ's spendable humor on a settled roll; then `click die <n>` spends it.
                var dice = _game.CliActiveDice;
                if (dice == null) return Fail("no dice roll on screen");
                int q = Array.FindIndex(DiceRollComponent.CliQueueNames, n => n.Equals(arg, StringComparison.OrdinalIgnoreCase));
                if (q < 0 && !int.TryParse(arg, out q)) return Fail("click humor paunch|hepar|spleen|pulmones");
                return dice.CliHumorButton(q) is { } hb ? Span(hb, $"humor {DiceRollComponent.CliQueueNames[q]}")
                                                        : Fail("no humor buttons on this roll (still rolling, or the body can spend none)");
            }

            case "die":
            {
                var dice = _game.CliActiveDice;
                if (dice == null) return Fail("no dice roll on screen");
                if (!int.TryParse(arg, out int di)) return Fail("click die <n>");
                return dice.CliDieCell(di) is { } dc2 ? At(dc2, $"die {di}") : Fail($"no die {di} on screen");
            }

            case "cell":
                if (a.Length < 3 || !int.TryParse(a[1], out int x) || !int.TryParse(a[2], out int y)) return Fail("click cell <x> <y>");
                return At((x, y), $"cell {x},{y}");
        }
        return Fail($"unknown click target '{what}'");
    }

    /// <summary>
    /// The pixel at the visual centre of cell (x, y). The terminal draws a cell centred on
    /// <c>offset + x * cellSize</c> (see <c>TerminalInputHandler.ScreenToCell</c>), so that is where it is
    /// both seen and hit.
    /// </summary>
    private Vector2 CellPixel(int x, int y)
    {
        var term = _game.CliTerminal!;
        var layout = term.GetLayoutInfo(Core.ClientSize);
        return new Vector2(layout.Offset.X + x * layout.CellSize.X, layout.Offset.Y + y * layout.CellSize.Y);
    }

    /// <summary>
    /// Where a hand lands on a button or a line of text: a little way in from its start rather than
    /// dead centre on a sixty-cell action, nudged a fraction of a cell differently each time so a run
    /// of clicks does not look like a machine's. The nudge is a function of the gesture count, so a
    /// replayed script lands in the same places.
    /// </summary>
    private Vector2 SpanPixel((int X, int Y, int W) s)
    {
        int into = s.W <= 3 ? s.W / 2 : Math.Min(s.W - 1, Math.Max(2, s.W / 4) + (_gesture % 3));
        var p = CellPixel(s.X + into, s.Y);
        var cell = _game.CliTerminal!.GetLayoutInfo(Core.ClientSize).CellSize;
        float jitter = ((_gesture * 37) % 7 - 3) / 10f;
        return p + new Vector2(jitter * cell.X, jitter * 0.4f * cell.Y);
    }

    /// <summary>The middle of the first occurrence of <paramref name="text"/> inside a rectangle of the terminal.</summary>
    private (int X, int Y)? FindText(string text, int left, int top, int right, int bottom)
    {
        var view = _game.CliTerminal?.View;
        if (view == null || text.Length == 0) return null;
        for (int y = top; y < Math.Min(bottom, view.Height); y++)
        {
            var row = new System.Text.StringBuilder();
            for (int x = 0; x < view.Width; x++) row.Append(view[x, y].Character is '\0' ? ' ' : view[x, y].Character);
            int at = row.ToString().IndexOf(text, StringComparison.OrdinalIgnoreCase);
            if (at >= left && at < right) return (at + Math.Min(text.Length / 2, 6), y);
        }
        return null;
    }

    private int ResolveMoon(string token)
    {
        if (int.TryParse(token, out int parsed)) return parsed;
        var (count, _, _) = _game.CliMoonState();
        for (int i = 0; i < count; i++)
            if (string.Equals(SkyMoons.Name(i), token, StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }
}
