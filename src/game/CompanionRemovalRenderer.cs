using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// Modal overlay shown at the start of the world-travel phase when the party holds more
/// companions than the protagonist's heart can sustain (see <see cref="MaxCompanionsStat"/>).
/// Presents a checklist of companions; the player ticks enough of them to dismiss so the
/// remaining party fits the limit, then confirms. Selected companions are then removed by
/// the host controller.
///
/// Purplish palette. Hovering a clickable row/button highlights it and (via the host) plays
/// a tick; toggling or confirming plays a click. Sound playback is delegated to the host
/// controller through the return values of <see cref="OnMouseMove"/> / <see cref="OnMouseClick"/>.
/// </summary>
public class CompanionRemovalRenderer
{
    /// <summary>Outcome of a click, so the host can play the right SFX / advance the phase.</summary>
    public enum ClickResult { None, Toggled, Confirmed }

    private readonly TerminalHUD _terminal;
    private readonly List<Companion> _companions;
    private readonly int _maxCompanions;
    private readonly HashSet<int> _selected = new();

    // -1 = nothing; 0..N-1 = companion rows; N = confirm button
    private int _hoveredRow = -1;

    // Layout, computed in Render and reused by hit-testing.
    private int _boxX, _boxY, _boxW, _boxH;
    private int _listStartY;
    private int _confirmY, _confirmX, _confirmW;

    private const int BoxWidth = 64;

    public CompanionRemovalRenderer(TerminalHUD terminal, List<Companion> companions, int maxCompanions)
    {
        _terminal      = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _companions    = companions ?? throw new ArgumentNullException(nameof(companions));
        _maxCompanions = Math.Max(0, maxCompanions);
    }

    /// <summary>Minimum companions that must be dismissed to satisfy the limit.</summary>
    public int RequiredRemovals => Math.Max(0, _companions.Count - _maxCompanions);

    /// <summary>True once enough companions are ticked to satisfy the limit.</summary>
    public bool CanConfirm => _selected.Count >= RequiredRemovals;

    /// <summary>Companions the player ticked for removal.</summary>
    public List<Companion> SelectedForRemoval =>
        _selected.OrderBy(i => i).Select(i => _companions[i]).ToList();

    // ── Palette (dark purple, matching the death-screen popup) ───
    private static Vector4 BoxBg        => new(0.06f, 0.0f, 0.10f, 1.0f);
    private static Vector4 HoverBg      => Config.Colors.DarkPurple;
    private static Vector4 TitleColor   => Config.Colors.BrightPurple;
    private static Vector4 BodyColor    => Config.Colors.LightPurpleGray;
    private static Vector4 DisabledColor => Config.Colors.Purple;

    // ── Rendering ────────────────────────────────────────────────

    /// <summary>Draws the full overlay. Cheap enough to call every frame.</summary>
    public void Render()
    {
        int n = _companions.Count;
        _boxW = BoxWidth;
        _boxH = n + 7;
        _boxX = Math.Max(0, (_terminal.Width  - _boxW) / 2);
        _boxY = Math.Max(0, (_terminal.Height - _boxH) / 2);
        _listStartY = _boxY + 4;
        _confirmY   = _boxY + 5 + n;

        _terminal.FillRect(_boxX, _boxY, _boxW, _boxH, ' ', BodyColor, BoxBg);
        DrawBorder();

        CenteredInBox(_boxY + 1, "— PARTING OF WAYS —", TitleColor, BoxBg);

        string instr = _maxCompanions == 0
            ? "Your heart can sustain no companions. Dismiss all."
            : $"Your heart sustains {_maxCompanions} companion(s) — dismiss at least {RequiredRemovals}.";
        CenteredInBox(_boxY + 2, Truncate(instr, _boxW - 4), BodyColor, BoxBg);

        for (int i = 0; i < n; i++) DrawRow(i);
        DrawConfirm();
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
        bool hovered  = _hoveredRow == i;
        bool selected = _selected.Contains(i);
        Vector4 bg = hovered ? HoverBg : BoxBg;

        _terminal.FillRect(x, y, w, 1, ' ', BodyColor, bg);

        string check = selected ? "[X]" : "[ ]";
        var c = _companions[i];
        string label = $"{check} {c.Name}";
        if (!string.IsNullOrWhiteSpace(c.Description))
            label += $" — {c.Description}";
        Vector4 fg = selected ? TitleColor : BodyColor;
        _terminal.Text(x, y, Truncate(label, w), fg, bg);
    }

    private void DrawConfirm()
    {
        bool enabled = CanConfirm;
        bool hovered = _hoveredRow == _companions.Count && enabled;
        string label = enabled
            ? $"[ CONFIRM — dismiss {_selected.Count} ]"
            : $"[ select {RequiredRemovals - _selected.Count} more ]";

        _confirmW = label.Length;
        _confirmX = _boxX + (_boxW - _confirmW) / 2;

        Vector4 bg = hovered ? HoverBg : BoxBg;
        Vector4 fg = enabled ? TitleColor : DisabledColor;
        // Clear the confirm row inside the borders, then draw the button label.
        _terminal.FillRect(_boxX + 1, _confirmY, _boxW - 2, 1, ' ', fg, BoxBg);
        _terminal.Text(_confirmX, _confirmY, label, fg, bg);
    }

    // ── Input ────────────────────────────────────────────────────

    /// <summary>Updates hover state. Returns true when the hovered element changed
    /// (so the host can play a tick).</summary>
    public bool OnMouseMove(int x, int y)
    {
        int newHover = HitTest(x, y);
        if (newHover == _hoveredRow) return false;

        int old = _hoveredRow;
        _hoveredRow = newHover;
        // Redraw affected elements only.
        RedrawElement(old);
        RedrawElement(newHover);
        return newHover != -1;
    }

    /// <summary>Handles a click. Toggles a companion or, on the confirm button, reports
    /// <see cref="ClickResult.Confirmed"/> when the selection is valid.</summary>
    public ClickResult OnMouseClick(int x, int y)
    {
        int hit = HitTest(x, y);
        if (hit < 0) return ClickResult.None;

        if (hit < _companions.Count)
        {
            if (!_selected.Add(hit)) _selected.Remove(hit);
            DrawRow(hit);
            DrawConfirm(); // enabled state / count may have changed
            return ClickResult.Toggled;
        }

        // Confirm button row.
        return CanConfirm ? ClickResult.Confirmed : ClickResult.None;
    }

    private void RedrawElement(int element)
    {
        if (element < 0) return;
        if (element < _companions.Count) DrawRow(element);
        else DrawConfirm();
    }

    /// <summary>Returns the element index under (x, y): 0..N-1 companion rows, N confirm, -1 none.</summary>
    private int HitTest(int x, int y)
    {
        // Companion rows.
        if (x >= _boxX + 2 && x < _boxX + _boxW - 2)
        {
            for (int i = 0; i < _companions.Count; i++)
                if (y == _listStartY + i) return i;
        }
        // Confirm button.
        if (y == _confirmY && x >= _confirmX && x < _confirmX + _confirmW)
            return _companions.Count;
        return -1;
    }

    // ── Helpers ──────────────────────────────────────────────────

    private void CenteredInBox(int y, string text, Vector4 fg, Vector4 bg)
    {
        int x = _boxX + (_boxW - text.Length) / 2;
        _terminal.Text(x, y, text, fg, bg);
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : (max <= 1 ? s.Substring(0, Math.Max(0, max)) : s.Substring(0, max - 1) + "…");
}
