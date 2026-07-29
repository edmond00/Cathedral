using System;
using OpenTK.Mathematics;
using Cathedral.Terminal;

namespace Cathedral.Game;

/// <summary>
/// Renders and manages the Settings screen on the TerminalHUD.
/// Shows two volume rows (Music, SFX), each adjusted via [ - ] / [ + ] step buttons,
/// plus a Back button. Reached from the main menu; active only during GameMode.Settings.
/// </summary>
public class SettingsMenuRenderer
{
    private readonly TerminalHUD _terminal;

    /// <summary>Music volume, 0–100. Initialize before Render().</summary>
    public int MusicVolume { get; set; } = 100;

    /// <summary>Sound-effects volume, 0–100. Initialize before Render().</summary>
    public int SfxVolume { get; set; } = 100;

    /// <summary>Fired with the new music volume (0–100) when a music button is clicked.</summary>
    public Action<int>? OnMusicVolumeChanged { get; set; }

    /// <summary>Fired with the new SFX volume (0–100) when an SFX button is clicked.</summary>
    public Action<int>? OnSfxVolumeChanged { get; set; }

    /// <summary>
    /// Whether the final full-screen dither layer is on. Covers the resting dither and
    /// the event pulses together — they are one effect. Initialize before Render().
    /// </summary>
    public bool DitherEnabled { get; set; } = true;

    /// <summary>Fired with the new state when the dither toggle is clicked.</summary>
    public Action<bool>? OnDitherChanged { get; set; }

    /// <summary>Fired when the Back button is clicked.</summary>
    public Action? OnBack { get; set; }

    // Control indices (also used as hover ids).
    private const int CtlMusicMinus = 0;
    private const int CtlMusicPlus  = 1;
    private const int CtlSfxMinus   = 2;
    private const int CtlSfxPlus    = 3;
    private const int CtlBack       = 4;
    private const int CtlDither     = 5;
    private int _hoveredControl = -1;

    private const int Step = 10; // percent per click

    // Layout
    private const int TitleRow   = 28;
    private const int MusicRow   = 40;
    private const int SfxRow     = 43;
    private const int DitherRow  = 46;
    private const int BackRow    = 51;
    private const int BarWidth   = 20;
    private const int RowWidth   = 47; // total width of a volume row (see column math below)
    private const string BackLabel = "[ Back ]";
    // Widest of the two states, so the hit region does not change size with the label.
    private const int ToggleW = 7; // "[ OFF ]"

    public SettingsMenuRenderer(TerminalHUD terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }

    /// <summary>Renders the full settings screen to the terminal.</summary>
    public void Render()
    {
        _terminal.Fill(' ', Config.Colors.Black, Config.Colors.Black);
        _terminal.Visible = true;

        const string ornament = "─ · ─ · ─ · ─ · ─ · ─ · ─ · ─";
        _terminal.CenteredText(TitleRow - 2, ornament, Config.Colors.DarkGray35, Config.Colors.Black);
        _terminal.CenteredText(TitleRow, "S E T T I N G S", Config.Colors.BrightYellow, Config.Colors.Black);
        _terminal.CenteredText(TitleRow + 2, ornament, Config.Colors.DarkGray35, Config.Colors.Black);

        DrawVolumeRow(MusicRow, "MUSIC", MusicVolume, CtlMusicMinus, CtlMusicPlus);
        DrawVolumeRow(SfxRow, "SFX", SfxVolume, CtlSfxMinus, CtlSfxPlus);
        DrawDitherRow();
        DrawBackButton();

        // Edge rules against the sphere, drawn last so nothing overwrites them
        _terminal.DrawSideRails();
    }

    // ── Column geometry (shared by render + hit-testing) ─────────────────────
    // label(7) sp(1) [ - ](5) sp(2) bar(20) sp(1) pct(4) sp(2) [ + ](5) = 47
    private int RowStartX => (_terminal.Width - RowWidth) / 2;
    private int MinusX => RowStartX + 8;
    private int BarX   => RowStartX + 15;
    private int PctX   => RowStartX + 36;
    private int PlusX  => RowStartX + 42;
    private const int BtnW = 5; // "[ - ]" / "[ + ]"

    private int BackStartX => (_terminal.Width - BackLabel.Length) / 2;

    private void DrawVolumeRow(int row, string label, int value, int minusCtl, int plusCtl)
    {
        int startX = RowStartX;
        _terminal.FillRect(startX, row, RowWidth, 1, ' ', Config.Colors.White, Config.Colors.Black);

        _terminal.Text(startX, row, label.PadRight(7), Config.Colors.MediumGray60, Config.Colors.Black);

        DrawStepButton(MinusX, row, "[ - ]", minusCtl, value > 0);
        DrawStepButton(PlusX, row, "[ + ]", plusCtl, value < 100);

        // Bar: filled cells proportional to value.
        int filled = (int)MathF.Round(value / 100f * BarWidth);
        for (int i = 0; i < BarWidth; i++)
        {
            bool on = i < filled;
            _terminal.Text(BarX + i, row, "█",
                on ? Config.Colors.BrightYellow : Config.Colors.DarkGray35, Config.Colors.Black);
        }

        _terminal.Text(PctX, row, $"{value,3}%", Config.Colors.White, Config.Colors.Black);
    }

    /// <summary>
    /// Dither on/off. Laid out on the same columns as a volume row — label at the left,
    /// toggle where the [ - ] button sits — so the three rows read as one stack.
    /// </summary>
    private void DrawDitherRow()
    {
        int startX = RowStartX;
        _terminal.FillRect(startX, DitherRow, RowWidth, 1, ' ', Config.Colors.White, Config.Colors.Black);

        _terminal.Text(startX, DitherRow, "DITHER".PadRight(7), Config.Colors.MediumGray60, Config.Colors.Black);

        bool hovered = _hoveredControl == CtlDither;
        Vector4 textColor = hovered      ? Config.Colors.BrightYellow
                          : DitherEnabled ? Config.Colors.White
                          :                 Config.Colors.DarkGray35;
        Vector4 bgColor = hovered ? Config.Colors.DarkYellow : Config.Colors.Black;

        string label = (DitherEnabled ? "[ ON ]" : "[ OFF ]").PadRight(ToggleW);
        _terminal.Text(MinusX, DitherRow, label, textColor, bgColor);
    }

    private void DrawStepButton(int x, int row, string text, int ctl, bool enabled)
    {
        Vector4 textColor, bgColor;
        if (!enabled)
        {
            textColor = Config.Colors.DarkGray35; bgColor = Config.Colors.Black;
        }
        else if (_hoveredControl == ctl)
        {
            textColor = Config.Colors.BrightYellow; bgColor = Config.Colors.DarkYellow;
        }
        else
        {
            textColor = Config.Colors.White; bgColor = Config.Colors.Black;
        }
        _terminal.Text(x, row, text, textColor, bgColor);
    }

    private void DrawBackButton()
    {
        bool hovered = _hoveredControl == CtlBack;
        Vector4 textColor = hovered ? Config.Colors.BrightYellow : Config.Colors.White;
        Vector4 bgColor   = hovered ? Config.Colors.DarkYellow   : Config.Colors.Black;
        _terminal.FillRect(BackStartX, BackRow, BackLabel.Length, 1, ' ', textColor, bgColor);
        _terminal.Text(BackStartX, BackRow, BackLabel, textColor, bgColor);
    }

    // ── Input ────────────────────────────────────────────────────────────────

    /// <summary>Updates hover state and redraws if it changed.</summary>
    public void OnMouseMove(int x, int y)
    {
        int newHovered = GetControlAtPosition(x, y);
        if (newHovered != _hoveredControl)
        {
            _hoveredControl = newHovered;
            Render();
        }
    }

    /// <summary>Handles a click: steps a volume or invokes Back.</summary>
    public void OnMouseClick(int x, int y)
    {
        switch (GetControlAtPosition(x, y))
        {
            case CtlMusicMinus: SetMusic(MusicVolume - Step); break;
            case CtlMusicPlus:  SetMusic(MusicVolume + Step); break;
            case CtlSfxMinus:   SetSfx(SfxVolume - Step); break;
            case CtlSfxPlus:    SetSfx(SfxVolume + Step); break;
            case CtlDither:     ToggleDither(); break;
            case CtlBack:       OnBack?.Invoke(); break;
        }
    }

    private void ToggleDither()
    {
        DitherEnabled = !DitherEnabled;
        OnDitherChanged?.Invoke(DitherEnabled);
        Render();
    }

    private void SetMusic(int v)
    {
        int clamped = Math.Clamp(v, 0, 100);
        if (clamped == MusicVolume) return;
        MusicVolume = clamped;
        OnMusicVolumeChanged?.Invoke(clamped);
        Render();
    }

    private void SetSfx(int v)
    {
        int clamped = Math.Clamp(v, 0, 100);
        if (clamped == SfxVolume) return;
        SfxVolume = clamped;
        OnSfxVolumeChanged?.Invoke(clamped);
        Render();
    }

    /// <summary>Returns the control index under (x, y), or -1.</summary>
    public int GetControlAtPosition(int x, int y)
    {
        if (y == MusicRow)
        {
            if (x >= MinusX && x < MinusX + BtnW) return CtlMusicMinus;
            if (x >= PlusX  && x < PlusX + BtnW)  return CtlMusicPlus;
        }
        else if (y == SfxRow)
        {
            if (x >= MinusX && x < MinusX + BtnW) return CtlSfxMinus;
            if (x >= PlusX  && x < PlusX + BtnW)  return CtlSfxPlus;
        }
        else if (y == DitherRow)
        {
            if (x >= MinusX && x < MinusX + ToggleW) return CtlDither;
        }
        else if (y == BackRow)
        {
            if (x >= BackStartX && x < BackStartX + BackLabel.Length) return CtlBack;
        }
        return -1;
    }

    /// <summary>Stable hover id for the controller's hover-tick logic, or null.</summary>
    public string? GetHoveredControlId(int x, int y) => GetControlAtPosition(x, y) switch
    {
        CtlMusicMinus => "settings:music-minus",
        CtlMusicPlus  => "settings:music-plus",
        CtlSfxMinus   => "settings:sfx-minus",
        CtlSfxPlus    => "settings:sfx-plus",
        CtlDither     => "settings:dither",
        CtlBack       => "settings:back",
        _             => null,
    };
}
