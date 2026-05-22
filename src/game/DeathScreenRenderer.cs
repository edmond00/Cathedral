using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Terminal;

namespace Cathedral.Game;

/// <summary>
/// Renders the death screen — a centered purple box with cause of death, a
/// narrative message, and an "[ END RUN ]" button.
/// </summary>
public sealed class DeathScreenRenderer
{
    private readonly TerminalHUD _terminal;

    // Layout
    private const int BoxW = 44;
    private const int BoxH = 16;

    private int _boxX;
    private int _boxY;
    private int _btnX;
    private int _btnY;
    private const string BtnLabel = "[ END RUN ]";
    private const int BtnW = 11; // BtnLabel.Length

    private int _hoverX = -1;
    private int _hoverY = -1;

    // Colors
    private static readonly Vector4 BgColor     = new(0.06f, 0.0f, 0.10f, 1.0f);
    private static readonly Vector4 BorderColor = Config.Colors.BrightPurple;
    private static readonly Vector4 TitleColor  = Config.Colors.BrightPurple;
    private static readonly Vector4 CauseColor  = Config.Colors.LightPurple;
    private static readonly Vector4 TextColor   = Config.Colors.LightPurpleGray;
    private static readonly Vector4 BtnText     = Config.Colors.BrightPurple;
    private static readonly Vector4 BtnHover    = Config.Colors.White;
    private static readonly Vector4 BtnBgHover  = Config.Colors.DarkPurple;

    public DeathScreenRenderer(TerminalHUD terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }

    public void SetHover(int cellX, int cellY)
    {
        _hoverX = cellX;
        _hoverY = cellY;
    }

    public bool IsOverEndRunButton(int cellX, int cellY)
        => cellY == _btnY && cellX >= _btnX && cellX < _btnX + BtnW;

    /// <summary>
    /// Renders the full death screen over the terminal.
    /// </summary>
    public void Draw(DeathCause cause)
    {
        _terminal.Visible = true;

        _boxX = (_terminal.Width  - BoxW) / 2;
        _boxY = (_terminal.Height - BoxH) / 2;

        // Background fill
        for (int dy = 0; dy < BoxH; dy++)
            for (int dx = 0; dx < BoxW; dx++)
                _terminal.SetCell(_boxX + dx, _boxY + dy, ' ', BgColor, BgColor);

        // Border (single-line box)
        DrawBox(_boxX, _boxY, BoxW, BoxH);

        int innerL = _boxX + 2;
        int innerW = BoxW - 4;
        int row    = _boxY + 1;

        // Title
        DrawCentered(row, "── YOU HAVE DIED ──", TitleColor);
        row += 2;

        // Cause subtitle
        DrawCentered(row, DeathMessages.GetTitle(cause), CauseColor);
        row += 2;

        // Decorative divider
        DrawCentered(row, new string('─', innerW), Config.Colors.Purple);
        row += 2;

        // Message lines (pre-split on \n)
        var msgLines = DeathMessages.GetMessage(cause).Split('\n');
        foreach (var line in msgLines)
        {
            DrawCentered(row, line, TextColor);
            row++;
        }

        // END RUN button (3 rows above bottom border)
        _btnY = _boxY + BoxH - 3;
        _btnX = _boxX + (BoxW - BtnW) / 2;

        bool hover = IsOverEndRunButton(_hoverX, _hoverY);
        _terminal.Text(_btnX, _btnY, BtnLabel,
            hover ? BtnHover  : BtnText,
            hover ? BtnBgHover : BgColor);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private void DrawCentered(int row, string text, Vector4 color)
    {
        int x = _boxX + (_boxW - text.Length) / 2;
        if (x < _boxX + 1) x = _boxX + 1;
        _terminal.Text(x, row, text, color, BgColor);
    }

    private int _boxW => BoxW;

    private void DrawBox(int x, int y, int w, int h)
    {
        // Corners
        _terminal.SetCell(x,         y,         '┌', BorderColor, BgColor);
        _terminal.SetCell(x + w - 1, y,         '┐', BorderColor, BgColor);
        _terminal.SetCell(x,         y + h - 1, '└', BorderColor, BgColor);
        _terminal.SetCell(x + w - 1, y + h - 1, '┘', BorderColor, BgColor);

        // Top / bottom
        for (int dx = 1; dx < w - 1; dx++)
        {
            _terminal.SetCell(x + dx, y,         '─', BorderColor, BgColor);
            _terminal.SetCell(x + dx, y + h - 1, '─', BorderColor, BgColor);
        }

        // Sides
        for (int dy = 1; dy < h - 1; dy++)
        {
            _terminal.SetCell(x,         y + dy, '│', BorderColor, BgColor);
            _terminal.SetCell(x + w - 1, y + dy, '│', BorderColor, BgColor);
        }
    }
}
