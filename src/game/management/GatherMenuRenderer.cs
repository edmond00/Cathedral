using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Management;

/// <summary>
/// The gathering phase, opened only by a Gather routine. The player picks how many days to stay at
/// the source with a slider; a live forecast shows how many attempts the stay offers, the dice each
/// attempt is rolled with and the yield they make likely. Staying plays the same time-passing beat as
/// the work menu — the bar refills while <see cref="GameClock"/> advances in step — and only then
/// rolls the attempts and fills the pack (<see cref="GatherYield.Run"/>), showing the yield with a
/// Continue button. Rendered like the work menu: a centered bordered box over the visible world.
/// ESC is NOT handled here — the launcher opens the pause menu.
/// </summary>
public sealed class GatherMenuRenderer
{
    private enum Phase { Configure, Staying, Done }

    // ── Duration range ────────────────────────────────────────────
    // A day takes what is there now; a year sees a wild source regrow four times and a cultivated one
    // once (ResourceRegen) — long enough for both to matter, short enough that the slider still
    // resolves single weeks.
    private const int MinDays = 1;
    private const int MaxDays = 360;
    private const int ArrowStepDays = 5;
    private const double StayAnimationSeconds = 2.0;
    private const double MaxFrameStepSeconds  = 0.1;

    // ── Layout ────────────────────────────────────────────────────
    private const int BarWidth  = 40;
    private const int BoxW      = 56;
    private const int ButtonGap = 6;

    // ── Colours (the work menu's, so the two time-passing menus read as one family) ──
    private static readonly Vector4 Outside  = Config.Colors.Transparent;
    private static readonly Vector4 Bg       = Config.Colors.Black;
    private static readonly Vector4 Border   = Config.Colors.DarkYellowGrey;
    private static readonly Vector4 Title    = Config.Colors.BrightYellow;
    private static readonly Vector4 Label    = Config.Colors.MediumGray60;
    private static readonly Vector4 Value    = Config.Colors.LightGray75;
    private static readonly Vector4 Sep      = Config.Colors.DarkGray35;
    private static readonly Vector4 Accent   = Config.Colors.BrightYellow;
    private static readonly Vector4 Good     = Config.Colors.White;
    private static readonly Vector4 Dim      = Config.Colors.DarkGray40;
    private static readonly Vector4 ChipBg   = Config.Colors.DarkGray20;
    private static readonly Vector4 BtnFg    = Config.TravelUI.ClearButtonTextColor;
    private static readonly Vector4 BtnBg    = Config.TravelUI.ClearButtonBackgroundColor;
    private static readonly Vector4 BtnHovFg = Config.TravelUI.ClearButtonHoverTextColor;
    private static readonly Vector4 BtnHovBg = Config.TravelUI.ClearButtonHoverBackgroundColor;
    private static readonly Vector4 OkFg     = Config.TravelUI.TravelButtonTextColor;
    private static readonly Vector4 OkBg     = Config.TravelUI.TravelButtonBackgroundColor;
    private static readonly Vector4 OkHovBg  = Config.TravelUI.TravelButtonHoverBackgroundColor;

    // ── Dependencies ──────────────────────────────────────────────
    private readonly TerminalHUD   _terminal;
    private readonly Protagonist   _protagonist;
    private readonly GatherRoutine _routine;
    private readonly PointOfInterest _source;
    private readonly Scene.Scene   _scene;
    private readonly IReadOnlyList<GatherSlot> _slots;
    private readonly int _dice;
    private readonly int _difficulty;
    private readonly double _regenDays;

    // ── State ─────────────────────────────────────────────────────
    private int    _days = 30;
    private Phase  _phase = Phase.Configure;
    private bool   _dragging;
    private double _startDay;
    private GatherResult? _result;
    private int _hoverX = -1, _hoverY = -1;

    private DateTime _lastTickUtc;
    private double   _animElapsed;
    private int      _daysAdvanced;

    // Box geometry + hit rects, computed each Render and reused by hit-testing.
    private int _boxX, _boxY, _boxH;
    private int _sliderRow = int.MinValue;
    private int _buttonsRow = int.MinValue;
    private int _leaveX0, _leaveX1;
    private int _confirmX0, _confirmX1;
    private int _continueRow = int.MinValue;
    private int _continueX0, _continueX1;

    /// <summary>Set once the player leaves the menu (stayed and continued, or left).</summary>
    public bool IsComplete { get; private set; }

    public GatherMenuRenderer(TerminalHUD terminal, Protagonist protagonist, GatherRoutine routine,
        Verb verb, PointOfInterest source, Scene.Scene scene)
    {
        _terminal    = terminal;
        _protagonist = protagonist;
        _routine     = routine;
        _source      = source;
        _scene       = scene;
        _slots       = GatherYield.Slots(routine, verb, source, scene, GameClock.Days);
        _dice        = GatherYield.Dice(routine, protagonist);
        _difficulty  = GatherYield.Difficulty(verb, source, _slots);
        _regenDays   = source.RegenDays;
    }

    private int BarX0 => _boxX + (BoxW - BarWidth) / 2;

    private GatherForecast Forecast => GatherYield.Forecast(_slots, GameClock.Days, _days, _dice, _difficulty);

    // ═══════════════════════════════════════════════════════════════
    // Input
    // ═══════════════════════════════════════════════════════════════

    public void OnMouseMove(int x, int y)
    {
        _hoverX = x; _hoverY = y;
        if (_phase != Phase.Configure) return;

        if (_terminal.IsLeftMouseDown)
        {
            if (_dragging || (y >= _sliderRow - 1 && y <= _sliderRow + 1 && x >= BarX0 - 1 && x <= BarX0 + BarWidth))
            {
                _dragging = true;
                SetDays(DaysAtX(x));
            }
        }
        else _dragging = false;
    }

    /// <summary>Stable identity of the clickable under (x, y) — the work menu's contract.</summary>
    public string? GetHoveredControlId(int x, int y)
    {
        if (_phase == Phase.Done)
            return y == _continueRow && x >= _continueX0 && x < _continueX1 ? "gather:continue" : null;
        if (_phase != Phase.Configure) return null;

        if (y == _buttonsRow)
        {
            if (x >= _confirmX0 && x < _confirmX1) return "gather:stay";
            if (x >= _leaveX0 && x < _leaveX1)     return "gather:leave";
        }
        if (y == _sliderRow)
        {
            if (x >= BarX0 - 4 && x < BarX0 - 1)                          return "gather:days-minus";
            if (x >= BarX0 + BarWidth + 1 && x < BarX0 + BarWidth + 4)    return "gather:days-plus";
            if (x >= BarX0 && x <= BarX0 + BarWidth)                      return "gather:days-bar";
        }
        return null;
    }

    public void OnMouseClick(int x, int y)
    {
        switch (_phase)
        {
            case Phase.Configure: ClickConfigure(x, y); break;
            case Phase.Done:      if (y == _continueRow && x >= _continueX0 && x < _continueX1) IsComplete = true; break;
        }
    }

    private void ClickConfigure(int x, int y)
    {
        if (y == _buttonsRow)
        {
            if (x >= _confirmX0 && x < _confirmX1) { BeginStay(); return; }
            if (x >= _leaveX0 && x < _leaveX1)     { IsComplete = true; return; }
        }
        if (y == _sliderRow)
        {
            if (x >= BarX0 - 4 && x < BarX0 - 1)                       { SetDays(_days - ArrowStepDays); return; }
            if (x >= BarX0 + BarWidth + 1 && x < BarX0 + BarWidth + 4) { SetDays(_days + ArrowStepDays); return; }
            if (x >= BarX0 && x <= BarX0 + BarWidth)                   { SetDays(DaysAtX(x)); return; }
        }
    }

    private int DaysAtX(int x)
    {
        double frac = (double)(x - BarX0) / BarWidth;
        return MinDays + (int)Math.Round(frac * (MaxDays - MinDays));
    }

    private void SetDays(int d) => _days = Math.Clamp(d, MinDays, MaxDays);

    /// <summary>
    /// Starts the stay. The day it starts on is captured now, so the attempts are dated from it however
    /// far the clock has moved by the time they are rolled. With animations off (<c>--playground</c>)
    /// the stay completes on the spot, so a script sees its result without waiting on a bar.
    /// </summary>
    private void BeginStay()
    {
        _phase        = Phase.Staying;
        _startDay     = GameClock.Days;
        _animElapsed  = 0.0;
        _daysAdvanced = 0;
        _lastTickUtc  = DateTime.UtcNow;
        if (Config.AnimationsAreInstant) FinishStay();
    }

    private void FinishStay()
    {
        if (_daysAdvanced < _days)
        {
            GameClock.Advance(_days - _daysAdvanced);
            _daysAdvanced = _days;
        }
        _result = GatherYield.Run(_slots, _scene, _protagonist, _startDay, _days, _dice, _difficulty);
        _phase  = Phase.Done;
    }

    // ── CLI seams: the same actions, addressed by meaning ──────────

    public bool CliSetDays(int days)
    {
        if (_phase != Phase.Configure) return false;
        SetDays(days);
        return true;
    }

    public bool CliStart()
    {
        if (_phase != Phase.Configure) return false;
        BeginStay();
        if (_phase == Phase.Staying) FinishStay();   // a script does not watch the bar fill
        return true;
    }

    public bool CliContinue()
    {
        if (_phase != Phase.Done) return false;
        IsComplete = true;
        return true;
    }

    public bool CliLeave()
    {
        if (_phase != Phase.Configure) return false;
        IsComplete = true;
        return true;
    }

    /// <summary>The menu's numbers as assertable lines — see <c>inspect gather</c>.</summary>
    public List<string> CliLines()
    {
        var inv = CultureInfo.InvariantCulture;
        if (_phase == Phase.Done && _result != null)
            return new List<string>
            {
                $"gather phase=done days={_result.Days} gathered={_result.Gathered} spoiled={_result.Spoiled} noroom={_result.NoRoom}",
            };

        var f = Forecast;
        return new List<string>
        {
            $"gather phase={(_phase == Phase.Configure ? "configure" : "staying")} days={_days} attempts={f.Attempts} "
          + $"dice={f.Dice} difficulty={f.Difficulty} chance={f.Chance.ToString("F3", inv)} expected={f.Expected} "
          + $"source=\"{_source.DisplayName}\" item={_routine.ItemId} slots={_slots.Count}",
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // Render
    // ═══════════════════════════════════════════════════════════════

    public void Render()
    {
        _terminal.Fill(' ', Config.Colors.White, Outside);
        switch (_phase)
        {
            case Phase.Configure: RenderConfigure(); break;
            case Phase.Staying:   RenderStaying();   break;
            case Phase.Done:      RenderDone();      break;
        }
    }

    private void RenderConfigure()
    {
        var f = Forecast;
        var lines = ForecastLines(f);

        // (blank) title subtitle <break> duration / blank / slider <break(yield)> lines… <break> buttons (blank)
        DrawBox(15 + lines.Count);

        int y = _boxY + 2;
        CenteredInBox(y++, Truncate($"Gathering at {_source.DisplayName}", BoxW - 4), Title, Bg);
        CenteredInBox(y, Truncate($"{_routine.ItemName} · {_routine.AreaName} · {_routine.Time.Label().ToLowerInvariant()}", BoxW - 4), Label, Bg);
        y++;
        DrawSectionBreak(ref y);

        _terminal.Text(_boxX + 2, y, "How long will you stay?", Label, Bg);
        string daysChip = $" {DaysText(_days)} ";
        _terminal.Text(_boxX + BoxW - 2 - daysChip.Length, y, daysChip, Accent, ChipBg);
        y += 2;

        _sliderRow = y;
        DrawArrow(BarX0 - 4, y, "[<]", _days > MinDays);
        int filled = (int)Math.Round((double)(_days - MinDays) / (MaxDays - MinDays) * BarWidth);
        bool overBar = _hoverY == y && _hoverX >= BarX0 && _hoverX <= BarX0 + BarWidth;
        for (int i = 0; i < BarWidth; i++)
        {
            bool hot = overBar && _hoverX == BarX0 + i;
            _terminal.Text(BarX0 + i, y, "█", hot ? Good : (i < filled ? Accent : Sep), Bg);
        }
        DrawArrow(BarX0 + BarWidth + 1, y, "[>]", _days < MaxDays);
        y++;

        DrawSectionBreak(ref y, "what the stay offers");
        foreach (var (text, fg) in lines)
            CenteredInBox(y++, Truncate(text, BoxW - 4), fg, Bg);

        DrawSectionBreak(ref y);
        DrawConfigureButtons(y);
    }

    private List<(string, Vector4)> ForecastLines(GatherForecast f)
    {
        var lines = new List<(string, Vector4)>
        {
            ($"{f.Attempts} attempt{(f.Attempts == 1 ? "" : "s")} — it grows back every {_regenDays:0} days",
             f.Attempts > 0 ? Value : Dim),
            ($"{f.Dice} dice, {f.Difficulty} six{(f.Difficulty == 1 ? "" : "es")} needed — {Percent(f.Chance)} each",
             f.Chance > 0 ? Value : Dim),
            (f.Chance <= 0 ? "beyond your dice — every attempt would spoil"
             : f.Attempts == 0 ? "nothing ripens in so short a stay"
             : f.Expected == 0 ? $"likely yield: perhaps one {_routine.ItemName}, likelier none"
             : $"likely yield: about {f.Expected} × {_routine.ItemName}",
             f.Chance > 0 && f.Attempts > 0 ? Good : Dim),
        };
        if (_routine.NeedsTool)
            lines.Add(($"with your {_routine.ToolItemName}", Label));
        return lines;
    }

    private void RenderStaying()
    {
        var now = DateTime.UtcNow;
        _animElapsed += Math.Clamp((now - _lastTickUtc).TotalSeconds, 0.0, MaxFrameStepSeconds);
        _lastTickUtc = now;

        double progress = Math.Clamp(_animElapsed / StayAnimationSeconds, 0.0, 1.0);
        int elapsedDays = (int)Math.Round(progress * _days);
        if (elapsedDays > _daysAdvanced)
        {
            GameClock.Advance(elapsedDays - _daysAdvanced);
            _daysAdvanced = elapsedDays;
        }

        DrawBox(7);
        int y = _boxY + 2;
        CenteredInBox(y, Truncate($"You stay at {_source.DisplayName}…", BoxW - 4), Title, Bg);
        y += 2;

        int filled = (int)Math.Round(progress * BarWidth);
        for (int i = 0; i < BarWidth; i++)
            _terminal.Text(BarX0 + i, y, "█", i < filled ? Accent : Sep, Bg);
        y += 2;
        CenteredInBox(y, $"{_daysAdvanced} / {DaysText(_days)}", Value, Bg);

        if (progress >= 1.0) FinishStay();
    }

    private void RenderDone()
    {
        if (_result == null) { IsComplete = true; return; }

        var lines = new List<(string text, Vector4 fg)>();
        if (_result.Gathered == 0)
            lines.Add(("Nothing gathered.", Dim));
        foreach (var (name, count) in _result.ByItem.OrderBy(kv => kv.Key))
            lines.Add(($"Gathered {count} × {name}", Good));
        if (_result.Spoiled > 0)
            lines.Add(($"{_result.Spoiled} spoiled by a clumsy hand", Label));
        if (_result.NoRoom > 0)
            lines.Add(($"{_result.NoRoom} left where they grew — no room to carry them", Label));

        DrawBox(10 + lines.Count);
        int y = _boxY + 2;
        CenteredInBox(y, $"— {DaysText(_days)} of gathering —", Title, Bg);
        y++;
        DrawSectionBreak(ref y);
        foreach (var (text, fg) in lines)
            CenteredInBox(y++, Truncate(text, BoxW - 4), fg, Bg);
        DrawSectionBreak(ref y);
        DrawContinue(y);
    }

    // ═══════════════════════════════════════════════════════════════
    // Widgets
    // ═══════════════════════════════════════════════════════════════

    private void DrawBox(int innerRows)
    {
        _boxH = innerRows + 2;
        _boxX = Math.Max(0, (_terminal.Width  - BoxW)  / 2);
        _boxY = Math.Max(0, (_terminal.Height - _boxH) / 2);

        _terminal.FillRect(_boxX, _boxY, BoxW, _boxH, ' ', Value, Bg);

        int x1 = _boxX + BoxW - 1, y1 = _boxY + _boxH - 1;
        for (int x = _boxX; x <= x1; x++)
        {
            _terminal.SetCell(x, _boxY, '─', Border, Bg);
            _terminal.SetCell(x, y1,    '─', Border, Bg);
        }
        for (int y = _boxY; y <= y1; y++)
        {
            _terminal.SetCell(_boxX, y, '│', Border, Bg);
            _terminal.SetCell(x1,    y, '│', Border, Bg);
        }
        _terminal.SetCell(_boxX, _boxY, '┌', Border, Bg);
        _terminal.SetCell(x1,    _boxY, '┐', Border, Bg);
        _terminal.SetCell(_boxX, y1,    '└', Border, Bg);
        _terminal.SetCell(x1,    y1,    '┘', Border, Bg);
    }

    private void DrawSectionBreak(ref int y, string? caption = null)
    {
        y++;
        DrawSeparator(y++, caption);
        y++;
    }

    private void DrawSeparator(int y, string? caption = null)
    {
        for (int x = _boxX + 1; x < _boxX + BoxW - 1; x++)
            _terminal.SetCell(x, y, '─', Sep, Bg);
        _terminal.SetCell(_boxX, y,            '├', Border, Bg);
        _terminal.SetCell(_boxX + BoxW - 1, y, '┤', Border, Bg);
        if (caption != null)
            CenteredInBox(y, $" {caption} ", Label, Bg);
    }

    private void DrawArrow(int x, int y, string label, bool enabled)
    {
        bool hov = enabled && _hoverY == y && _hoverX >= x && _hoverX < x + label.Length;
        Vector4 fg = enabled ? (hov ? BtnHovFg : Value) : Dim;
        Vector4 bg = hov ? BtnHovBg : Bg;
        _terminal.Text(x, y, label, fg, bg);
    }

    private void DrawConfigureButtons(int y)
    {
        const string leaveLabel   = "[ Leave ]";
        const string confirmLabel = "[ Stay ]";
        _buttonsRow = y;

        int total = leaveLabel.Length + ButtonGap + confirmLabel.Length;
        int x = _boxX + (BoxW - total) / 2;

        _leaveX0 = x; _leaveX1 = x + leaveLabel.Length;
        bool hovLeave = _hoverY == y && _hoverX >= _leaveX0 && _hoverX < _leaveX1;
        _terminal.Text(_leaveX0, y, leaveLabel, hovLeave ? BtnHovFg : BtnFg, hovLeave ? BtnHovBg : BtnBg);

        _confirmX0 = _leaveX1 + ButtonGap; _confirmX1 = _confirmX0 + confirmLabel.Length;
        bool hovOk = _hoverY == y && _hoverX >= _confirmX0 && _hoverX < _confirmX1;
        _terminal.Text(_confirmX0, y, confirmLabel, OkFg, hovOk ? OkHovBg : OkBg);
    }

    private void DrawContinue(int y)
    {
        const string label = "[ Continue ]";
        _continueRow = y;
        int x = _boxX + (BoxW - label.Length) / 2;
        bool hov = _hoverY == y && _hoverX >= x && _hoverX < x + label.Length;
        _terminal.Text(x, y, label, OkFg, hov ? OkHovBg : OkBg);
        _continueX0 = x;
        _continueX1 = x + label.Length;
    }

    private void CenteredInBox(int y, string text, Vector4 fg, Vector4 bg)
    {
        int x = _boxX + (BoxW - text.Length) / 2;
        _terminal.Text(x, y, text, fg, bg);
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : (max <= 1 ? s.Substring(0, Math.Max(0, max)) : s.Substring(0, max - 1) + "…");

    private static string DaysText(int d) => d == 1 ? "1 day" : $"{d} days";

    private static string Percent(double p)
        => p <= 0 ? "no chance"
         : p >= 0.995 ? "certain"
         : p < 0.01 ? "under 1%"
         : $"{Math.Round(p * 100):0}%";
}
