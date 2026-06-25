using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative.Routines;

namespace Cathedral.Game;

/// <summary>
/// Modal world-view overlay listing the routines recorded for a travel destination. Routines that
/// fail virtual replay are greyed out and cannot be selected. Replayable routines are clickable to
/// start a routine-replay travel; a RETURN button dismisses the box back to the travel plan.
/// Modeled on <see cref="CompanionRemovalRenderer"/> (cell-coordinate hit-testing, host-driven SFX).
/// </summary>
public class TravelRoutinesBox
{
    public enum ResultKind { None, Return, Selected }

    /// <summary>One listed routine plus its virtual-replay verdict.</summary>
    public sealed class Entry
    {
        public Routine Routine { get; init; } = null!;
        public bool Replayable { get; init; }
        public string? Reason { get; init; }
    }

    private readonly TerminalHUD _terminal;
    private readonly List<Entry> _entries;

    // -1 = nothing; 0..N-1 = routine rows; N = return button
    private int _hoveredRow = -1;

    private int _boxX, _boxY, _boxW, _boxH;
    private int _listStartY;
    private int _returnY, _returnX, _returnW;

    private const int BoxWidth = 64;

    public TravelRoutinesBox(TerminalHUD terminal, List<Entry> entries)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _entries  = entries ?? throw new ArgumentNullException(nameof(entries));
    }

    private static Vector4 BoxBg         => new(0.0f, 0.06f, 0.10f, 1.0f);
    private static Vector4 HoverBg       => Config.Colors.DarkPurple;
    private static Vector4 TitleColor    => Config.Colors.BrightPurple;
    private static Vector4 BodyColor     => Config.Colors.LightPurpleGray;
    private static Vector4 DisabledColor => Config.Colors.Purple;

    public void Render()
    {
        int n = Math.Max(1, _entries.Count);
        _boxW = BoxWidth;
        _boxH = n + 7;
        _boxX = Math.Max(0, (_terminal.Width  - _boxW) / 2);
        _boxY = Math.Max(0, (_terminal.Height - _boxH) / 2);
        _listStartY = _boxY + 4;
        _returnY    = _boxY + 5 + n;

        _terminal.FillRect(_boxX, _boxY, _boxW, _boxH, ' ', BodyColor, BoxBg);
        DrawBorder();

        CenteredInBox(_boxY + 1, "— ROUTINES —", TitleColor, BoxBg);
        CenteredInBox(_boxY + 2, Truncate("Replay a learned routine at this location.", _boxW - 4), BodyColor, BoxBg);

        if (_entries.Count == 0)
            CenteredInBox(_listStartY, "(no routines for this destination)", DisabledColor, BoxBg);
        else
            for (int i = 0; i < _entries.Count; i++) DrawRow(i);

        DrawReturn();
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

    private void DrawRow(int i)
    {
        int y = _listStartY + i;
        int x = _boxX + 2;
        int w = _boxW - 4;
        var e = _entries[i];
        bool hovered = _hoveredRow == i && e.Replayable;
        Vector4 bg = hovered ? HoverBg : BoxBg;

        _terminal.FillRect(x, y, w, 1, ' ', BodyColor, bg);

        string label = e.Routine.Name;
        if (!e.Replayable && !string.IsNullOrWhiteSpace(e.Reason))
            label += $"  ({e.Reason})";
        Vector4 fg = e.Replayable ? (hovered ? TitleColor : BodyColor) : DisabledColor;
        _terminal.Text(x, y, Truncate(label, w), fg, bg);
    }

    private void DrawReturn()
    {
        bool hovered = _hoveredRow == _entries.Count;
        const string label = "[ RETURN ]";
        _returnW = label.Length;
        _returnX = _boxX + (_boxW - _returnW) / 2;
        Vector4 bg = hovered ? HoverBg : BoxBg;
        _terminal.FillRect(_boxX + 1, _returnY, _boxW - 2, 1, ' ', BodyColor, BoxBg);
        _terminal.Text(_returnX, _returnY, label, TitleColor, bg);
    }

    public bool OnMouseMove(int x, int y)
    {
        int newHover = HitTest(x, y);
        if (newHover == _hoveredRow) return false;
        int old = _hoveredRow;
        _hoveredRow = newHover;
        RedrawElement(old);
        RedrawElement(newHover);
        return newHover != -1;
    }

    /// <summary>Returns the click outcome; for <see cref="ResultKind.Selected"/> the routine is set.</summary>
    public (ResultKind kind, Routine? routine) OnMouseClick(int x, int y)
    {
        int hit = HitTest(x, y);
        if (hit < 0) return (ResultKind.None, null);
        if (hit == _entries.Count) return (ResultKind.Return, null);

        var e = _entries[hit];
        return e.Replayable ? (ResultKind.Selected, e.Routine) : (ResultKind.None, null);
    }

    private void RedrawElement(int element)
    {
        if (element < 0) return;
        if (element < _entries.Count) DrawRow(element);
        else DrawReturn();
    }

    private int HitTest(int x, int y)
    {
        if (x >= _boxX + 2 && x < _boxX + _boxW - 2)
        {
            for (int i = 0; i < _entries.Count; i++)
                if (y == _listStartY + i) return i;
        }
        if (y == _returnY && x >= _returnX && x < _returnX + _returnW)
            return _entries.Count;
        return -1;
    }

    private void CenteredInBox(int y, string text, Vector4 fg, Vector4 bg)
    {
        int x = _boxX + (_boxW - text.Length) / 2;
        _terminal.Text(x, y, text, fg, bg);
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : (max <= 1 ? s.Substring(0, Math.Max(0, max)) : s.Substring(0, max - 1) + "…");
}
