using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Creation;

namespace Cathedral.Game.Management;

/// <summary>
/// Renders the protagonist's learned routines in the management menu (protagonist-only, like the
/// journal). Each routine row carries a clickable ☑ (locked) / ☐ (unlocked) toggle that protects
/// the routine from FIFO eviction. Shows current queue usage against the anamnesis-derived capacity.
/// </summary>
public class RoutinesPanelRenderer
{
    private readonly TerminalHUD _terminal;

    // Hit rows map a screen row to an index in the protagonist's RecordedRoutines list.
    private readonly List<(int row, int routineIndex)> _hitRows = new();
    private int _hoveredRow = -1;

    private const int ListStartRow = 6;
    private const int MaxRows = 80;

    public RoutinesPanelRenderer(TerminalHUD terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }

    /// <summary>True while a routine row is hovered (for SFX feedback).</summary>
    public bool IsHovering => _hoveredRow >= 0;

    public void ClearHover() => _hoveredRow = -1;

    public void Render(Protagonist protagonist)
    {
        _hitRows.Clear();
        int x = BodyArtViewer.PanelContentX;

        _terminal.Text(x, 1, "R O U T I N E S", Config.Colors.BrightYellow, Config.Colors.Black);
        _terminal.Text(x, 3, "──────────────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);

        int max  = protagonist.GetRoutineQueueSize();
        int used = protagonist.RecordedRoutines.Count;
        _terminal.Text(x, 4, $"Queue: {used} / {max}", Config.Colors.LightGray75, Config.Colors.Black);

        if (protagonist.RecordedRoutines.Count == 0)
        {
            _terminal.Text(x, ListStartRow, "No routines learned yet.", Config.Colors.DarkGray35, Config.Colors.Black);
            _terminal.Text(x, ListStartRow + 2, "Succeed at recordable actions", Config.Colors.DarkGray35, Config.Colors.Black);
            _terminal.Text(x, ListStartRow + 3, "during narration to learn one.", Config.Colors.DarkGray35, Config.Colors.Black);
            return;
        }

        // Newest first.
        int rowsShown = 0;
        for (int i = protagonist.RecordedRoutines.Count - 1; i >= 0 && rowsShown < MaxRows; i--, rowsShown++)
        {
            int row = ListStartRow + rowsShown;
            var routine = protagonist.RecordedRoutines[i];
            bool hovered = _hoveredRow == row;

            string box   = routine.Locked ? "☑" : "☐";
            string label = $"{box} {routine.Name}";
            if (label.Length > 32) label = label.Substring(0, 31) + "…";

            Vector4 fg = routine.Locked
                ? Config.Colors.BrightYellow
                : (hovered ? Config.Colors.MediumYellow : Config.Colors.MediumGray60);
            Vector4 bg = hovered ? new Vector4(0.07f, 0.06f, 0.01f, 1.0f) : Config.Colors.Black;

            _terminal.FillRect(x, row, 34, 1, ' ', fg, bg);
            _terminal.Text(x, row, label, fg, bg);

            _hitRows.Add((row, i));
        }
    }

    public bool ProcessHover(int x, int y)
    {
        int newHover = HitTest(x, y);
        if (newHover == _hoveredRow) return false;
        _hoveredRow = newHover;
        return true;
    }

    /// <summary>Toggles the lock on the clicked routine. Returns true when a routine was toggled.</summary>
    public bool ProcessClick(int x, int y, Protagonist protagonist)
    {
        int idx = HitTestIndex(x, y);
        if (idx < 0 || idx >= protagonist.RecordedRoutines.Count) return false;
        protagonist.RecordedRoutines[idx].Locked = !protagonist.RecordedRoutines[idx].Locked;
        return true;
    }

    private int HitTest(int x, int y)
    {
        int left = BodyArtViewer.PanelContentX;
        if (x < left || x >= left + 34) return -1;
        foreach (var (row, _) in _hitRows)
            if (y == row) return row;
        return -1;
    }

    private int HitTestIndex(int x, int y)
    {
        int left = BodyArtViewer.PanelContentX;
        if (x < left || x >= left + 34) return -1;
        foreach (var (row, routineIndex) in _hitRows)
            if (y == row) return routineIndex;
        return -1;
    }
}
