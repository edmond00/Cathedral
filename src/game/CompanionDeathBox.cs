using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Terminal;

namespace Cathedral.Game;

/// <summary>
/// Modal world-view overlay announcing that one or more companions have died of old age, shown when
/// the party returns to the world map and the age check finds them past their lifetime. Lists each
/// companion and the age they reached; a single CONTINUE button dismisses it. The companions are
/// already out of the party by the time this is shown — the box only reports it.
///
/// Deliberately purple throughout: the palette the game reserves for wounds, defeat and death
/// (see <c>Config.Colors.Purple</c>), so the news reads as loss at a glance rather than as an
/// ordinary outcome box.
/// </summary>
public class CompanionDeathBox
{
    private readonly TerminalHUD _terminal;
    private readonly List<string> _lines;

    private bool _continueHovered;

    private int _boxX, _boxY, _boxW, _boxH;
    private int _continueY, _continueX, _continueW;

    private const int BoxWidth = 64;

    /// <param name="lines">One line per departed companion, e.g. "Maren — died at 27400 days".</param>
    public CompanionDeathBox(TerminalHUD terminal, List<string> lines)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _lines    = lines ?? new List<string>();
    }

    private static Vector4 BoxBg      => new(0.08f, 0.0f, 0.12f, 1.0f);
    private static Vector4 HoverBg    => Config.Colors.DarkPurple;
    private static Vector4 TitleColor => Config.Colors.BrightPurple;
    private static Vector4 BodyColor  => Config.Colors.LightPurpleGray;
    private static Vector4 NoteColor  => Config.Colors.Purple;

    public void Render()
    {
        int n = Math.Max(1, _lines.Count);
        _boxW = BoxWidth;
        _boxH = n + 9;
        _boxX = Math.Max(0, (_terminal.Width  - _boxW) / 2);
        _boxY = Math.Max(0, (_terminal.Height - _boxH) / 2);
        _continueY = _boxY + 7 + n;

        _terminal.FillRect(_boxX, _boxY, _boxW, _boxH, ' ', BodyColor, BoxBg);
        DrawBorder();

        string title = _lines.Count > 1 ? "— THEY HAVE DIED OF OLD AGE —" : "— DIED OF OLD AGE —";
        CenteredInBox(_boxY + 1, title, TitleColor, BoxBg);

        for (int i = 0; i < _lines.Count; i++)
            _terminal.Text(_boxX + 3, _boxY + 3 + i, Truncate(_lines[i], _boxW - 6), BodyColor, BoxBg);

        int noteRow = _boxY + 4 + n;
        CenteredInBox(noteRow,     "The organs ceased their labour,", NoteColor, BoxBg);
        CenteredInBox(noteRow + 1, "one by one, in the quiet dark.",  NoteColor, BoxBg);

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
