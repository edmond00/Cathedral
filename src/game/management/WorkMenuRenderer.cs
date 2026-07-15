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
/// Full-screen work menu, opened after a successful request-job dialogue. The player drags a slider
/// (30 … 1800 days) to choose how long to work; a live preview shows the coins and modus-mentis XP
/// the stint would earn (and which unknown skills it would let them learn). Confirming advances the
/// game clock, credits the coins, applies the XP/learning, then shows a results box with a Continue button.
/// </summary>
public sealed class WorkMenuRenderer
{
    private enum Phase { Configure, Working, Done }

    // ── Duration range ────────────────────────────────────────────
    private const int MinDays  = 30;
    private const int MaxDays  = 1800;
    /// <summary>Days added/removed by one click of the ◄/► arrows.</summary>
    private const int ArrowStepDays = 10;
    private const double WorkAnimationSeconds = 1.3;

    // ── Layout ────────────────────────────────────────────────────
    private const int TitleRow    = 6;
    private const int WalletRow    = 9;
    private const int DurLabelRow  = 14;
    private const int SliderRow    = 16;
    private const int PreviewRow   = 21;   // coins line; skills follow
    private const int MessageRow   = 34;
    private const int ConfirmRow   = 38;
    private const int LeaveRow      = 41;

    private const int BarWidth = 40;

    // ── Colours ───────────────────────────────────────────────────
    private static readonly Vector4 Bg       = Config.Colors.Black;
    private static readonly Vector4 Title    = Config.Colors.BrightYellow;
    private static readonly Vector4 Label    = Config.Colors.MediumGray60;
    private static readonly Vector4 Value    = Config.Colors.LightGray75;
    private static readonly Vector4 Sep      = Config.Colors.DarkGray35;
    private static readonly Vector4 Accent   = Config.Colors.BrightYellow;
    private static readonly Vector4 Good     = Config.Colors.OrangeYellow;
    private static readonly Vector4 Dim      = Config.Colors.DarkGray40;
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
    private DateTime _workStartUtc;
    private WorkOutcome? _result;
    private int _hoverX = -1, _hoverY = -1;
    private int _confirmX0, _confirmX1;
    private int _continueX0, _continueX1;

    /// <summary>Set once the player leaves the menu (confirmed + continued, or cancelled).</summary>
    public bool IsComplete { get; private set; }

    public WorkMenuRenderer(TerminalHUD terminal, Protagonist protagonist, NpcEntity npc, Job job)
    {
        _terminal    = terminal;
        _protagonist = protagonist;
        _party       = protagonist.Party;
        _npc         = npc;
        _job         = job;
    }

    private int BarX0 => (Config.Terminal.MainWidth - BarWidth) / 2;

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
            if (_dragging || (y >= SliderRow - 1 && y <= SliderRow + 1 && x >= BarX0 - 1 && x <= BarX0 + BarWidth))
            {
                _dragging = true;
                SetDays(DaysAtX(x));
            }
        }
        else _dragging = false;
    }

    public void OnKeyEscape()
    {
        if (_phase == Phase.Configure) IsComplete = true;
        else if (_phase == Phase.Done) IsComplete = true;
    }

    public void OnMouseClick(int x, int y)
    {
        switch (_phase)
        {
            case Phase.Configure: ClickConfigure(x, y); break;
            case Phase.Done:      if (y == ContinueRow && x >= _continueX0 && x < _continueX1) IsComplete = true; break;
        }
    }

    private void ClickConfigure(int x, int y)
    {
        // Confirm.
        if (y == ConfirmRow && x >= _confirmX0 && x < _confirmX1) { BeginWork(); return; }
        // Leave.
        if (y == LeaveRow && x >= BarX0 && x < BarX0 + LeaveLabel.Length) { IsComplete = true; return; }
        // Step arrows.
        if (y == SliderRow)
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

    private void BeginWork()
    {
        _result       = WorkOutcome.Apply(_job, _days, _protagonist, _party);
        GameClock.Advance(_days);
        _phase        = Phase.Working;
        _workStartUtc = DateTime.UtcNow;
    }

    // ═══════════════════════════════════════════════════════════════
    // Render
    // ═══════════════════════════════════════════════════════════════

    private const string LeaveLabel = "[ Leave ]";
    private int ContinueRow => MessageRow + 6;

    public void Render()
    {
        _terminal.Clear();

        switch (_phase)
        {
            case Phase.Configure: RenderConfigure(); break;
            case Phase.Working:   RenderWorking();   break;
            case Phase.Done:      RenderDone();      break;
        }
    }

    private void RenderConfigure()
    {
        _terminal.CenteredText(TitleRow, $"Working for {_npc.DisplayName} as {_job.WithArticle()}", Title, Bg);
        RenderWallet(WalletRow);

        _terminal.CenteredText(DurLabelRow, "How long will you work?", Label, Bg);

        // Slider: [<]  ██████░░░░░░  [>]
        DrawArrow(BarX0 - 4, SliderRow, "[<]", _days > MinDays);
        int filled = (int)Math.Round((double)(_days - MinDays) / (MaxDays - MinDays) * BarWidth);
        for (int i = 0; i < BarWidth; i++)
            _terminal.Text(BarX0 + i, SliderRow, "█", i < filled ? Accent : Sep, Bg);
        DrawArrow(BarX0 + BarWidth + 1, SliderRow, "[>]", _days < MaxDays);
        _terminal.CenteredText(SliderRow + 2, DaysText(_days), Value, Bg);

        // Outcome preview.
        var preview = WorkOutcome.Preview(_job, _days, _protagonist);
        int row = PreviewRow;
        _terminal.CenteredText(row, "— you would earn —", Label, Bg);
        row += 2;

        string coins = preview.Coins > 0
            ? $"{preview.Coins}{CoinGlyph(preview.Coin)} ({CoinName(preview.Coin)})"
            : "no coin yet — work longer";
        _terminal.CenteredText(row, coins, preview.Coins > 0 ? CoinColor(preview.Coin) : Dim, Bg);
        row += 2;

        foreach (var s in preview.Skills)
        {
            _terminal.CenteredText(row, PreviewSkillLine(s), SkillColor(s), Bg);
            row++;
        }

        DrawConfirm();
        DrawLeave();
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
        if (!s.WasKnown) return s.Learned ? Good : Dim;
        return s.LevelsGained > 0 ? Good : Value;
    }

    private void RenderWorking()
    {
        _terminal.CenteredText(TitleRow + 6, $"You work as {_job.WithArticle()} for {_npc.DisplayName}…", Title, Bg);
        _terminal.CenteredText(TitleRow + 8, $"{DaysText(_days)} pass.", Value, Bg);

        if ((DateTime.UtcNow - _workStartUtc).TotalSeconds >= WorkAnimationSeconds)
            _phase = Phase.Done;
    }

    private void RenderDone()
    {
        if (_result == null) { IsComplete = true; return; }

        _terminal.CenteredText(TitleRow, $"— {DaysText(_days)} of work done —", Title, Bg);
        int row = TitleRow + 3;

        if (_result.Coins > 0)
        {
            _terminal.CenteredText(row, $"Earned {_result.Coins}{CoinGlyph(_result.Coin)} ({CoinName(_result.Coin)})",
                CoinColor(_result.Coin), Bg);
            row += 2;
        }

        foreach (var s in _result.Skills)
        {
            _terminal.CenteredText(row, ResultSkillLine(s), SkillColor(s), Bg);
            row++;
            if (s.Dropped != null)
            {
                _terminal.CenteredText(row, $"  (you set aside {s.Dropped.DisplayName} to make room)", Dim, Bg);
                row++;
            }
        }

        DrawContinue();
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

    private void DrawArrow(int x, int y, string label, bool enabled)
    {
        bool hov = enabled && _hoverY == y && _hoverX >= x && _hoverX < x + label.Length;
        Vector4 fg = enabled ? (hov ? BtnHovFg : Value) : Dim;
        Vector4 bg = hov ? BtnHovBg : Bg;
        _terminal.Text(x, y, label, fg, bg);
    }

    private void DrawConfirm()
    {
        string label = "[ Confirm — start work ]";
        int x = (Config.Terminal.MainWidth - label.Length) / 2;
        bool hov = _hoverY == ConfirmRow && _hoverX >= x && _hoverX < x + label.Length;
        _terminal.Text(x, ConfirmRow, label, OkFg, hov ? OkHovBg : OkBg);
        _confirmX0 = x;
        _confirmX1 = x + label.Length;
    }

    private void DrawLeave()
    {
        bool hov = _hoverY == LeaveRow && _hoverX >= BarX0 && _hoverX < BarX0 + LeaveLabel.Length;
        _terminal.Text(BarX0, LeaveRow, LeaveLabel, hov ? BtnHovFg : BtnFg, hov ? BtnHovBg : BtnBg);
        _terminal.Text(BarX0 + LeaveLabel.Length + 2, LeaveRow, "(or press Esc)", Label, Bg);
    }

    private void DrawContinue()
    {
        string label = "[ Continue ]";
        int x = (Config.Terminal.MainWidth - label.Length) / 2;
        bool hov = _hoverY == ContinueRow && _hoverX >= x && _hoverX < x + label.Length;
        _terminal.Text(x, ContinueRow, label, OkFg, hov ? OkHovBg : OkBg);
        _continueX0 = x;
        _continueX1 = x + label.Length;
    }

    private void RenderWallet(int row)
    {
        (string text, Vector4 color)[] segs =
        {
            ($"{_party.Gold}{Config.Symbols.GoldCoinSymbol}",     Config.Colors.CoinGold),
            ($"{_party.Silver}{Config.Symbols.SilverCoinSymbol}", Config.Colors.CoinSilver),
            ($"{_party.Copper}{Config.Symbols.CopperCoinSymbol}", Config.Colors.CoinCopper),
        };
        const int gap = 2;
        int len = segs.Sum(s => s.text.Length) + gap * (segs.Length - 1);
        int x = (Config.Terminal.MainWidth - len) / 2;
        foreach (var s in segs)
        {
            _terminal.Text(x, row, s.text, s.color, Bg);
            x += s.text.Length + gap;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

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
