using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Work;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Management;

/// <summary>
/// Work menu, opened after a successful request-job dialogue. Rendered as a centered bordered box
/// (black interior, transparent surround so the 3D world stays visible behind it). The player drags
/// a slider (30 … 1800 days) to choose how long to work; a live preview shows the coins and
/// modus-mentis XP the stint would earn (and which unknown skills it would let them learn).
///
/// Confirming plays the time-passing beat: the duration bar the player just set refills while a day
/// counter ticks up, and <see cref="GameClock"/> is advanced in step with it so the world's date
/// tracks what is on screen. Only once the bar is full is the outcome applied (coins credited, XP
/// awarded, skills learned) and the results box shown with a Continue button.
/// ESC is NOT handled here — the launcher opens the pause menu.
/// </summary>
public sealed class WorkMenuRenderer
{
    private enum Phase { Configure, Working, Done }

    // ── Duration range ────────────────────────────────────────────
    private const int MinDays  = 30;
    private const int MaxDays  = 1800;
    /// <summary>Days added/removed by one click of the ◄/► arrows.</summary>
    private const int ArrowStepDays = 10;
    /// <summary>Real seconds the whole stint takes to play out, whatever its length in days.</summary>
    private const double WorkAnimationSeconds = 2.0;
    /// <summary>Largest real-time step one frame may contribute. Caps the catch-up after a pause
    /// (ESC overlay, unfocused window) so the animation resumes instead of snapping to its end.</summary>
    private const double MaxFrameStepSeconds = 0.1;

    // ── Layout ────────────────────────────────────────────────────
    private const int BarWidth = 40;
    private const int BoxW     = 56;
    private const int ButtonGap = 6;

    // ── Colours ───────────────────────────────────────────────────
    private static readonly Vector4 Outside  = Config.Colors.Transparent;   // world shows through
    private static readonly Vector4 Bg       = Config.Colors.Black;
    private static readonly Vector4 Border   = Config.Colors.DarkYellowGrey;
    private static readonly Vector4 Title    = Config.Colors.BrightYellow;
    private static readonly Vector4 Label    = Config.Colors.MediumGray60;
    private static readonly Vector4 Value    = Config.Colors.LightGray75;
    private static readonly Vector4 Sep      = Config.Colors.DarkGray35;
    private static readonly Vector4 Accent   = Config.Colors.BrightYellow;
    private static readonly Vector4 Learn    = Config.Colors.White;
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
    private readonly TerminalHUD _terminal;
    private readonly Protagonist _protagonist;
    private readonly Party       _party;
    private readonly NpcEntity   _npc;
    private readonly Job         _job;

    // ── State ─────────────────────────────────────────────────────
    private int     _days = MinDays;
    private Phase   _phase   = Phase.Configure;
    private bool    _dragging;
    private WorkOutcome? _result;
    private int _hoverX = -1, _hoverY = -1;

    // Time-passing animation: accumulated (not wall-clock) elapsed seconds, and how much of the
    // stint has already been fed to GameClock so the two never drift apart.
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

    /// <summary>Set once the player leaves the menu (confirmed + continued, or left).</summary>
    public bool IsComplete { get; private set; }

    public WorkMenuRenderer(TerminalHUD terminal, Protagonist protagonist, NpcEntity npc, Job job)
    {
        _terminal    = terminal;
        _protagonist = protagonist;
        _party       = protagonist.Party;
        _npc         = npc;
        _job         = job;
    }

    private int BarX0 => _boxX + (BoxW - BarWidth) / 2;

    // ═══════════════════════════════════════════════════════════════
    // Input
    // ═══════════════════════════════════════════════════════════════

    public void OnMouseMove(int x, int y)
    {
        _hoverX = x; _hoverY = y;

        if (_phase != Phase.Configure) return;

        // Drag the slider while the left button is held over the bar.
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

    /// <summary>
    /// Stable identity of the clickable element under (x, y), or null when there is none — the same
    /// contract <c>SettingsMenuRenderer.GetHoveredControlId</c> has, so the controller's one
    /// "tick when the hovered element changes" rule covers this screen too. Like the trade menu, it
    /// had no tick at all.
    ///
    /// <para>Mirrors <see cref="ClickConfigure"/> and the Done branch of <see cref="OnMouseClick"/>.
    /// The bar reports one id along its whole length rather than one per day, so dragging the slider
    /// is silent instead of a rattle — the arrows either side are the discrete controls.</para>
    /// </summary>
    public string? GetHoveredControlId(int x, int y)
    {
        if (_phase == Phase.Done)
            return y == _continueRow && x >= _continueX0 && x < _continueX1 ? "work:continue" : null;

        if (_phase != Phase.Configure) return null;   // mid-work: nothing is clickable

        if (y == _buttonsRow)
        {
            if (x >= _confirmX0 && x < _confirmX1) return "work:confirm";
            if (x >= _leaveX0 && x < _leaveX1)     return "work:leave";
        }

        if (y == _sliderRow)
        {
            if (x >= BarX0 - 4 && x < BarX0 - 1) return "work:days-minus";
            if (x >= BarX0 + BarWidth + 1 && x < BarX0 + BarWidth + 4) return "work:days-plus";
            if (x >= BarX0 && x <= BarX0 + BarWidth) return "work:days-bar";
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
        // Buttons.
        if (y == _buttonsRow)
        {
            if (x >= _confirmX0 && x < _confirmX1) { BeginWork(); return; }
            if (x >= _leaveX0 && x < _leaveX1)     { IsComplete = true; return; }
        }
        // Step arrows + bar.
        if (y == _sliderRow)
        {
            if (x >= BarX0 - 4 && x < BarX0 - 1) { SetDays(_days - ArrowStepDays); return; }
            if (x >= BarX0 + BarWidth + 1 && x < BarX0 + BarWidth + 4) { SetDays(_days + ArrowStepDays); return; }
            if (x >= BarX0 && x <= BarX0 + BarWidth) { SetDays(DaysAtX(x)); return; }
        }
    }

    private int DaysAtX(int x)
    {
        double frac = (double)(x - BarX0) / BarWidth;
        return MinDays + (int)Math.Round(frac * (MaxDays - MinDays));
    }

    private void SetDays(int d) => _days = Math.Clamp(d, MinDays, MaxDays);

    /// <summary>Starts the time-passing beat. The outcome is applied only once it finishes, so the
    /// coins and XP land with the results box rather than before the bar has drawn a single frame.</summary>
    private void BeginWork()
    {
        _phase        = Phase.Working;
        _animElapsed  = 0.0;
        _daysAdvanced = 0;
        _lastTickUtc  = DateTime.UtcNow;
    }

    // ═══════════════════════════════════════════════════════════════
    // Render
    // ═══════════════════════════════════════════════════════════════

    public void Render()
    {
        // Transparent surround: the (dimmed) 3D world stays visible outside the box.
        _terminal.Fill(' ', Config.Colors.White, Outside);

        switch (_phase)
        {
            case Phase.Configure: RenderConfigure(); break;
            case Phase.Working:   RenderWorking();   break;
            case Phase.Done:      RenderDone();      break;
        }
    }

    private void RenderConfigure()
    {
        var preview = WorkOutcome.Preview(_job, _days, _protagonist);
        int skillRows = preview.Skills.Count;

        // (blank) title <break> duration / blank / slider <break(earn)> coins / blank / skills… <break> buttons (blank)
        DrawBox(18 + skillRows);

        int y = _boxY + 2;   // one blank row under the top border
        CenteredInBox(y, Truncate($"Working for {_npc.DisplayName} as {_job.WithArticle()}", BoxW - 4), Title, Bg);
        y++;
        DrawSectionBreak(ref y);

        // Duration: label left, chosen value right (chip background).
        _terminal.Text(_boxX + 2, y, "How long will you work?", Label, Bg);
        string daysChip = $" {DaysText(_days)} ";
        _terminal.Text(_boxX + BoxW - 2 - daysChip.Length, y, daysChip, Accent, ChipBg);
        y += 2;   // blank row between the question and the slider

        // Slider: [<]  ██████░░░░░░  [>]
        _sliderRow = y;
        DrawArrow(BarX0 - 4, y, "[<]", _days > MinDays);
        int filled = (int)Math.Round((double)(_days - MinDays) / (MaxDays - MinDays) * BarWidth);
        bool overBar = _hoverY == y && _hoverX >= BarX0 && _hoverX <= BarX0 + BarWidth;
        for (int i = 0; i < BarWidth; i++)
        {
            // Highlight the cell the cursor would set if clicked here.
            bool hot = overBar && _hoverX == BarX0 + i;
            _terminal.Text(BarX0 + i, y, "█", hot ? Learn : (i < filled ? Accent : Sep), Bg);
        }
        DrawArrow(BarX0 + BarWidth + 1, y, "[>]", _days < MaxDays);
        y++;

        DrawSectionBreak(ref y, "you would earn");

        string coins = preview.Coins > 0
            ? $"{preview.Coins}{CoinGlyph(preview.Coin)} ({CoinName(preview.Coin)})"
            : "no coin yet — work longer";
        CenteredInBox(y, coins, preview.Coins > 0 ? CoinColor(preview.Coin) : Dim, Bg);
        y += 2;   // blank row between the salary and the skill list

        foreach (var s in preview.Skills)
        {
            CenteredInBox(y, Truncate(PreviewSkillLine(s), BoxW - 4), SkillColor(s), Bg);
            y++;
        }

        DrawSectionBreak(ref y);
        DrawConfigureButtons(y);
    }

    private static string PreviewSkillLine(WorkMmResult s)
    {
        if (s.WasKnown)
        {
            string lvl = s.AtMaxLevel ? " (at mastery)"
                       : s.LevelsGained > 0 ? $" → +{s.LevelsGained} level{(s.LevelsGained == 1 ? "" : "s")}" : "";
            return $"{s.DisplayName}: +{s.XpGained} xp{lvl}";
        }
        return s.Learned
            ? $"{s.DisplayName}: would be learned (level 1)"
            : $"{s.DisplayName}: +{s.XpGained} xp — too little to learn yet";
    }

    private static Vector4 SkillColor(WorkMmResult s)
    {
        if (!s.WasKnown) return s.Learned ? Learn : Dim;
        return s.LevelsGained > 0 ? Learn : Value;
    }

    /// <summary>
    /// The time-passing beat: the duration bar refills while the day counter climbs to the chosen
    /// stint. <see cref="GameClock"/> is advanced by the difference each frame — never in one jump —
    /// so it lands on exactly the chosen number of days when the bar completes.
    /// </summary>
    private void RenderWorking()
    {
        var now = DateTime.UtcNow;
        _animElapsed += Math.Clamp((now - _lastTickUtc).TotalSeconds, 0.0, MaxFrameStepSeconds);
        _lastTickUtc = now;

        double progress = Math.Clamp(_animElapsed / WorkAnimationSeconds, 0.0, 1.0);

        int elapsedDays = (int)Math.Round(progress * _days);
        if (elapsedDays > _daysAdvanced)
        {
            GameClock.Advance(elapsedDays - _daysAdvanced);
            _daysAdvanced = elapsedDays;
        }

        // (blank) title (blank) bar (blank) counter (blank)
        DrawBox(7);
        int y = _boxY + 2;
        CenteredInBox(y, Truncate($"You work as {_job.WithArticle()} for {_npc.DisplayName}…", BoxW - 4), Title, Bg);
        y += 2;

        // Same geometry as the duration slider, so it reads as that bar filling up.
        int filled = (int)Math.Round(progress * BarWidth);
        for (int i = 0; i < BarWidth; i++)
            _terminal.Text(BarX0 + i, y, "█", i < filled ? Accent : Sep, Bg);
        y += 2;

        CenteredInBox(y, $"{_daysAdvanced} / {DaysText(_days)}", Value, Bg);

        if (progress >= 1.0)
        {
            _result = WorkOutcome.Apply(_job, _days, _protagonist, _party);
            _phase  = Phase.Done;
        }
    }

    private void RenderDone()
    {
        if (_result == null) { IsComplete = true; return; }

        var lines = new List<(string text, Vector4 fg)>();
        if (_result.Coins > 0)
            lines.Add(($"Earned {_result.Coins}{CoinGlyph(_result.Coin)} ({CoinName(_result.Coin)})", CoinColor(_result.Coin)));
        foreach (var s in _result.Skills)
        {
            lines.Add((ResultSkillLine(s), SkillColor(s)));
            if (s.Dropped != null)
                lines.Add(($"(you set aside {s.Dropped.DisplayName} to make room)", Dim));
        }

        // (blank) title <break> lines… <break> continue (blank)
        DrawBox(10 + lines.Count);

        int y = _boxY + 2;   // one blank row under the top border
        CenteredInBox(y, $"— {DaysText(_days)} of work done —", Title, Bg);
        y++;
        DrawSectionBreak(ref y);

        foreach (var (text, fg) in lines)
        {
            CenteredInBox(y, Truncate(text, BoxW - 4), fg, Bg);
            y++;
        }

        DrawSectionBreak(ref y);
        DrawContinue(y);
    }

    private static string ResultSkillLine(WorkMmResult s)
    {
        if (s.WasKnown)
        {
            string lvl = s.LevelsGained > 0 ? $" — gained {s.LevelsGained} level{(s.LevelsGained == 1 ? "" : "s")}" : "";
            return $"{s.DisplayName}: +{s.XpGained} xp{lvl}";
        }
        return s.Learned
            ? $"{s.DisplayName}: learned! (level 1)"
            : $"{s.DisplayName}: nothing learned";
    }

    // ═══════════════════════════════════════════════════════════════
    // Widgets
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Fills and frames the centered box; <paramref name="innerRows"/> excludes the borders.</summary>
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

    /// <summary>Section rule with one blank row of breathing room either side; advances <paramref name="y"/> past all three.</summary>
    private void DrawSectionBreak(ref int y, string? caption = null)
    {
        y++;                        // blank above
        DrawSeparator(y++, caption);
        y++;                        // blank below
    }

    /// <summary>Horizontal rule across the box, optionally with a centered caption.</summary>
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
        const string confirmLabel = "[ Start work ]";
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

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    private void CenteredInBox(int y, string text, Vector4 fg, Vector4 bg)
    {
        int x = _boxX + (BoxW - text.Length) / 2;
        _terminal.Text(x, y, text, fg, bg);
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : (max <= 1 ? s.Substring(0, Math.Max(0, max)) : s.Substring(0, max - 1) + "…");

    private static string DaysText(int d) => d == 1 ? "1 day" : $"{d} days";

    private static char CoinGlyph(CoinType c) => c switch
    {
        CoinType.Gold   => Config.Symbols.GoldCoinSymbol,
        CoinType.Silver => Config.Symbols.SilverCoinSymbol,
        _               => Config.Symbols.CopperCoinSymbol,
    };

    private static Vector4 CoinColor(CoinType c) => c switch
    {
        CoinType.Gold   => Config.Colors.CoinGold,
        CoinType.Silver => Config.Colors.CoinSilver,
        _               => Config.Colors.CoinCopper,
    };

    private static string CoinName(CoinType c) => c switch
    {
        CoinType.Gold   => "gold",
        CoinType.Silver => "silver",
        _               => "copper",
    };
}
