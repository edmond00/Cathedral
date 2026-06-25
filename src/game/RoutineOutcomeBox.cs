using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Terminal;

namespace Cathedral.Game;

/// <summary>
/// Modal world-view overlay shown after a routine is fully replayed. Lists the routine name and the
/// outcomes produced (acquired items, used resources) plus a note about what happens next (entering
/// a new area, a fight, a dialogue, or simply returning to travel). A single CONTINUE button
/// dismisses it; the host then applies the routine's final phase transition.
/// </summary>
public class RoutineOutcomeBox
{
    private readonly TerminalHUD _terminal;
    private readonly string _title;
    private readonly List<string> _lines;

    private bool _continueHovered;

    private int _boxX, _boxY, _boxW, _boxH;
    private int _continueY, _continueX, _continueW;

    private const int BoxWidth = 64;

    public RoutineOutcomeBox(TerminalHUD terminal, string title, List<string> lines)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _title    = title ?? "";
        _lines    = lines ?? new List<string>();
    }

    private static Vector4 BoxBg      => new(0.0f, 0.06f, 0.10f, 1.0f);
    private static Vector4 HoverBg    => Config.Colors.DarkPurple;
    private static Vector4 TitleColor => Config.Colors.BrightPurple;
    private static Vector4 BodyColor  => Config.Colors.LightPurpleGray;

    public void Render()
    {
        int n = Math.Max(1, _lines.Count);
        _boxW = BoxWidth;
        _boxH = n + 7;
        _boxX = Math.Max(0, (_terminal.Width  - _boxW) / 2);
        _boxY = Math.Max(0, (_terminal.Height - _boxH) / 2);
        _continueY = _boxY + 5 + n;

        _terminal.FillRect(_boxX, _boxY, _boxW, _boxH, ' ', BodyColor, BoxBg);
        DrawBorder();

        CenteredInBox(_boxY + 1, "— ROUTINE COMPLETE —", TitleColor, BoxBg);
        CenteredInBox(_boxY + 2, Truncate(_title, _boxW - 4), BodyColor, BoxBg);

        if (_lines.Count == 0)
            CenteredInBox(_boxY + 4, "(nothing of note)", BodyColor, BoxBg);
        else
            for (int i = 0; i < _lines.Count; i++)
                _terminal.Text(_boxX + 3, _boxY + 4 + i, Truncate(_lines[i], _boxW - 6), BodyColor, BoxBg);

        DrawContinue();
    }

    private void DrawBorder()
    {
        int x0 = _boxX, y0 = _boxY, x1 = _boxX + _boxW - 1, y1 = _boxY + _boxH - 1;
        for (int x = x0; x <= x1; x++)
        {
            _terminal.SetCell(x, y0, '─', TitleColor, BoxBg);
            _terminal.SetCell(x, y1, '─', TitleColor, BoxBg);
        }
        for (int y = y0; y <= y1; y++)
        {
            _terminal.SetCell(x0, y, '│', TitleColor, BoxBg);
            _terminal.SetCell(x1, y, '│', TitleColor, BoxBg);
        }
        _terminal.SetCell(x0, y0, '┌', TitleColor, BoxBg);
        _terminal.SetCell(x1, y0, '┐', TitleColor, BoxBg);
        _terminal.SetCell(x0, y1, '└', TitleColor, BoxBg);
        _terminal.SetCell(x1, y1, '┘', TitleColor, BoxBg);
    }

    private void DrawContinue()
    {
        const string label = "[ CONTINUE ]";
        _continueW = label.Length;
        _continueX = _boxX + (_boxW - _continueW) / 2;
        Vector4 bg = _continueHovered ? HoverBg : BoxBg;
        _terminal.FillRect(_boxX + 1, _continueY, _boxW - 2, 1, ' ', BodyColor, BoxBg);
        _terminal.Text(_continueX, _continueY, label, TitleColor, bg);
    }

    /// <summary>Updates hover; returns true when the hover state changed.</summary>
    public bool OnMouseMove(int x, int y)
    {
        bool nowHovered = y == _continueY && x >= _continueX && x < _continueX + _continueW;
        if (nowHovered == _continueHovered) return false;
        _continueHovered = nowHovered;
        DrawContinue();
        return nowHovered;
    }

    /// <summary>Returns true when CONTINUE was clicked.</summary>
    public bool OnMouseClick(int x, int y)
        => y == _continueY && x >= _continueX && x < _continueX + _continueW;

    private void CenteredInBox(int y, string text, Vector4 fg, Vector4 bg)
    {
        int x = _boxX + (_boxW - text.Length) / 2;
        _terminal.Text(x, y, text, fg, bg);
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : (max <= 1 ? s.Substring(0, Math.Max(0, max)) : s.Substring(0, max - 1) + "…");
}
