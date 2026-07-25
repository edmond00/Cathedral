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
/// journal).
///
/// Layout:
///   Right column (col 70+): the scrollable routine list. Each row carries a clickable
///     ☑ (locked) / ☐ (unlocked) checkbox that protects the routine from FIFO eviction; clicking
///     the name selects the routine.
///   Center pane (cols ~17–66) for the selected routine, in three stacked zones:
///     TOP    — general info: name, lock state, start time, requirements, outcomes.
///     MIDDLE — a transparent "porthole" rectangle that reveals the always-rendered 3D world
///              behind the HUD; the host drives the world camera to focus on the routine's
///              location so it reads as a minimap (see <see cref="OnRoutineFocused"/>).
///     BOTTOM — the ordered list of steps in the routine.
/// </summary>
public class RoutinesPanelRenderer
{
    private readonly TerminalHUD _terminal;

    // Hit rows map a screen row to an index in the protagonist's RecordedRoutines list.
    private readonly List<(int row, int routineIndex)> _hitRows = new();
    private int _hoveredRow = -1;
    // Screen row whose lock/unlock checkbox the mouse is currently over (-1 = none). Tracked
    // separately from _hoveredRow so the checkbox can highlight even on a non-selected routine.
    private int _hoveredCheckboxRow = -1;

    // Currently selected routine (index into RecordedRoutines), or -1 when none/empty.
    private int _selectedIndex = -1;
    private int _lastFocusedLocationId = int.MinValue;

    private const int ListStartRow = 6;
    private const int MaxRows = 80;

    // List column geometry (right panel).
    private static int ListX => BodyArtViewer.PanelContentX;   // col 70
    private const int ListWidth = 30;                          // clamps within the 100-wide grid
    private const int CheckboxCols = 2;                        // first 2 cells = lock toggle hit area

    // Center pane geometry (between the left nav separator at col 15 and the right list at col 70).
    private const int CenterLeft = 17;
    private const int CenterRight = 66;
    private const int CenterWidth = CenterRight - CenterLeft + 1;

    // Vertical separator between the center pane and the right list (matches the other tabs' seam).
    private const int SeparatorX = BodyArtViewer.PanelX - 1;   // col 67

    // Half-extent (in cells) of the porthole band above/below the viewport center.
    private const int PortholeHalf = 16;

    // Highlight background for a lock/unlock checkbox the mouse is hovering.
    private static readonly Vector4 CheckboxHoverBg = new(0.18f, 0.15f, 0.02f, 1.0f);

    /// <summary>
    /// Fired when the selected routine changes (and on activation), with the routine's LocationId.
    /// The host uses this to center the world camera so the minimap porthole shows that location.
    /// </summary>
    public Action<int>? OnRoutineFocused;

    public RoutinesPanelRenderer(TerminalHUD terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }

    /// <summary>True while a routine row is hovered (for SFX feedback).</summary>
    public bool IsHovering => _hoveredRow >= 0;

    public void ClearHover() { _hoveredRow = -1; _hoveredCheckboxRow = -1; }

    /// <summary>
    /// Called when the Routines tab becomes active. Establishes a default selection (newest routine)
    /// and fires <see cref="OnRoutineFocused"/> so the host focuses the minimap camera.
    /// </summary>
    public void OnActivated(Protagonist protagonist)
    {
        if (protagonist.RecordedRoutines.Count == 0)
        {
            _selectedIndex = -1;
            return;
        }

        if (_selectedIndex < 0 || _selectedIndex >= protagonist.RecordedRoutines.Count)
            _selectedIndex = protagonist.RecordedRoutines.Count - 1; // newest

        FireFocus(protagonist, force: true);
    }

    public void Render(Protagonist protagonist)
    {
        _hitRows.Clear();
        int x = ListX;

        // Vertical seam between the center pane and the right list (drawn regardless of contents).
        for (int y = 0; y < _terminal.Height; y++)
            _terminal.SetCell(SeparatorX, y, '│', Config.Colors.DarkGray35, Config.Colors.Black);

        // ── Right column: list header + queue gauge ──────────────────
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

        // Keep selection valid (e.g. after FIFO eviction shifted the list).
        if (_selectedIndex < 0 || _selectedIndex >= protagonist.RecordedRoutines.Count)
            _selectedIndex = protagonist.RecordedRoutines.Count - 1;

        // ── Right column: list rows (newest first) ───────────────────
        int rowsShown = 0;
        for (int i = protagonist.RecordedRoutines.Count - 1; i >= 0 && rowsShown < MaxRows; i--, rowsShown++)
        {
            int row = ListStartRow + rowsShown;
            var routine = protagonist.RecordedRoutines[i];
            bool hovered  = _hoveredRow == row;
            bool selected = _selectedIndex == i;

            string box   = routine.Locked ? "☑" : "☐";
            string label = $"{box} {routine.Name}";
            if (label.Length > ListWidth) label = label.Substring(0, ListWidth - 1) + "…";

            Vector4 fg = selected
                ? Config.Colors.BrightYellow
                : routine.Locked
                    ? Config.Colors.MediumYellow
                    : (hovered ? Config.Colors.MediumYellow : Config.Colors.MediumGray60);
            Vector4 bg = (selected || hovered) ? new Vector4(0.07f, 0.06f, 0.01f, 1.0f) : Config.Colors.Black;

            _terminal.FillRect(x, row, ListWidth, 1, ' ', fg, bg);
            _terminal.Text(x, row, label, fg, bg);

            // Highlight the clickable lock/unlock checkbox when the mouse is over it — on any row,
            // selected or not, since clicking it toggles the routine's lock.
            if (_hoveredCheckboxRow == row)
            {
                _terminal.FillRect(x, row, CheckboxCols, 1, ' ', Config.Colors.BrightYellow, CheckboxHoverBg);
                _terminal.Text(x, row, box, Config.Colors.BrightYellow, CheckboxHoverBg);
            }

            _hitRows.Add((row, i));
        }

        // ── Center pane: detail for the selected routine ─────────────
        RenderCenterPane(protagonist.RecordedRoutines[_selectedIndex]);
    }

    // ═══════════════════════════════════════════════════════════════
    // Center pane
    // ═══════════════════════════════════════════════════════════════

    private void RenderCenterPane(Routine routine)
    {
        // The three zones are anchored to the porthole band in the middle.
        int cy         = _terminal.Height / 2;   // 50
        int bandTop    = cy - PortholeHalf;       // 34
        int bandBottom = cy + PortholeHalf;       // 66

        RenderTopInfo(routine);

        // Dotted separators bracket the middle band, dividing the three zones.
        DottedHLine(CenterLeft, CenterRight, bandTop - 1, Config.Colors.DarkGray35);
        RenderMiddleBand(bandTop, bandBottom);
        DottedHLine(CenterLeft, CenterRight, bandBottom + 1, Config.Colors.DarkGray35);

        RenderStepList(routine, bandBottom + 2);
    }

    /// <summary>TOP zone: name, lock state, start time, requirements, outcomes.</summary>
    private void RenderTopInfo(Routine routine)
    {
        int x = CenterLeft;
        int row = 1;

        CenterText(x, row++, routine.Name, Config.Colors.BrightYellow);
        row++;

        string lockText = routine.Locked ? "☑ Locked" : "☐ Unlocked";
        CenterText(x, row++, lockText, routine.Locked ? Config.Colors.MediumYellow : Config.Colors.MediumGray60);
        CenterText(x, row++, $"Start: {routine.StartTime.Label()}    Steps: {routine.Steps.Count}", Config.Colors.LightGray75);
        row++;

        // Requirements (deduped across all steps).
        CenterText(x, row++, "REQUIREMENTS", Config.Colors.White);
        var reqs = CollectRequirements(routine);
        if (reqs.Count == 0)
            CenterText(x, row++, "  · none", Config.Colors.DarkGray35);
        else
            foreach (var r in reqs)
                CenterText(x, row++, "  · " + r, Config.Colors.MediumGray60);
        row++;

        // Outcomes.
        CenterText(x, row++, "OUTCOMES", Config.Colors.White);
        foreach (var o in CollectOutcomes(routine))
            CenterText(x, row++, "  · " + o, Config.Colors.MediumGray60);
    }

    /// <summary>
    /// MIDDLE zone: a transparent "porthole" onto the always-rendered 3D world, filling the full
    /// center-pane width so the whole band shows the viewport. The host aims the world camera at the
    /// routine's location so it lands at the viewport center (cell (Width/2, Height/2)). There is no
    /// margin or drawn border — the dotted separators above and below frame it. A small yellow
    /// corner-box marks the center while leaving the center cell — and the cell directly above it —
    /// fully transparent.
    /// </summary>
    private void RenderMiddleBand(int bandTop, int bandBottom)
    {
        int cx = _terminal.Width / 2;   // 50
        int cy = _terminal.Height / 2;  // 50

        // The whole band is the porthole — transparent across the full center-pane width.
        for (int y = bandTop; y <= bandBottom; y++)
            for (int sx = CenterLeft; sx <= CenterRight; sx++)
                _terminal.SetCell(sx, y, ' ', Config.Colors.Transparent, Config.Colors.Transparent);

        // Small yellow corner-box around the center. Only the diagonal corners are drawn, so the
        // center cell and the cell directly above it stay fully transparent.
        _terminal.SetCell(cx - 1, cy - 1, '┌', Config.Colors.BrightYellow, Config.Colors.Transparent);
        _terminal.SetCell(cx + 1, cy - 1, '┐', Config.Colors.BrightYellow, Config.Colors.Transparent);
        _terminal.SetCell(cx - 1, cy + 1, '└', Config.Colors.BrightYellow, Config.Colors.Transparent);
        _terminal.SetCell(cx + 1, cy + 1, '┘', Config.Colors.BrightYellow, Config.Colors.Transparent);
    }

    /// <summary>Draws a dotted horizontal rule (a dash every other cell) across [x0, x1] on row y.</summary>
    private void DottedHLine(int x0, int x1, int y, Vector4 color)
    {
        for (int gx = x0; gx <= x1; gx += 2)
            _terminal.SetCell(gx, y, '─', color, Config.Colors.Black);
    }

    /// <summary>BOTTOM zone: the ordered steps of the routine, starting just below the middle band.</summary>
    private void RenderStepList(Routine routine, int startRow)
    {
        int x = CenterLeft;
        int row = startRow + 1;   // one empty row before the STEPS header

        CenterText(x, row++, "STEPS", Config.Colors.White);

        for (int i = 0; i < routine.Steps.Count && row < _terminal.Height - 1; i++)
        {
            var step = routine.Steps[i];
            string phaseMark = step.TriggeredPhase switch
            {
                RoutinePhaseKind.Fight    => " ⚔",
                RoutinePhaseKind.Dialogue => " 💬",
                _                          => "",
            };
            CenterText(x, row++, $"{i + 1}. {step.Verbatim}{phaseMark}", Config.Colors.MediumGray60);
        }
    }

    private static List<string> CollectRequirements(Routine routine)
    {
        var seen = new HashSet<string>();
        var list = new List<string>();
        void Add(string s) { if (seen.Add(s)) list.Add(s); }

        foreach (var step in routine.Steps)
            foreach (var c in step.Constraints)
            {
                switch (c)
                {
                    case ItemConstraint item when !string.IsNullOrWhiteSpace(item.ItemName):
                        Add($"item: {item.ItemName}");
                        break;
                    case ActingMemberConstraint actor when !string.IsNullOrWhiteSpace(actor.MemberKey):
                        Add($"with: {actor.MemberKey}");
                        break;
                    case ModusMentisConstraint mm when !string.IsNullOrWhiteSpace(mm.ModusMentisId):
                        Add($"modus mentis: {mm.ModusMentisId}");
                        break;
                }
            }

        return list;
    }

    /// <summary>The concrete result of each step, deduped and in order (e.g. "move to the clearing",
    /// "gain some moss", "start a fight").</summary>
    private static List<string> CollectOutcomes(Routine routine)
    {
        var seen = new HashSet<string>();
        var list = new List<string>();
        void Add(string s) { if (!string.IsNullOrWhiteSpace(s) && seen.Add(s)) list.Add(s); }

        foreach (var step in routine.Steps)
            Add(DescribeStepOutcome(step));

        if (list.Count == 0) list.Add("no lasting change");
        return list;
    }

    /// <summary>
    /// A human-readable result for one recorded step, derived from its verb and target. Steps that
    /// hand off to another phase describe that transition; the rest map their verb to the concrete
    /// world change (moving somewhere, gaining an item, …). Falls back to the recorded verbatim.
    /// </summary>
    private static string DescribeStepOutcome(RoutineStep step)
    {
        switch (step.TriggeredPhase)
        {
            case RoutinePhaseKind.Fight:    return $"start a fight with {StepTargetName(step)}";
            case RoutinePhaseKind.Dialogue: return $"start a dialogue with {StepTargetName(step)}";
        }

        string t = StepTargetName(step);
        return step.VerbId switch
        {
            "move"           => $"move to {t}",
            "follow_path"    => $"take {t}",
            "climb_up"       => $"climb up {t}",
            "climb_down"     => $"climb down {t}",
            "go_up_stairs"   => $"go up {t}",
            "go_down_stairs" => $"go down {t}",
            "open_door"      => $"go through {t}",
            "enter_spot"     => $"reach {t}",
            "leave"          => $"leave {t}",
            "gather" or "grab" or "steal" or "cut" => $"gain {t}",
            _                => step.Verbatim,
        };
    }

    /// <summary>The step target's recorded display name, or a neutral "it" when none was captured.</summary>
    private static string StepTargetName(RoutineStep step)
    {
        string n = step.Target?.DisplayName?.Trim() ?? "";
        return n.Length == 0 ? "it" : n;
    }

    // ═══════════════════════════════════════════════════════════════
    // Input
    // ═══════════════════════════════════════════════════════════════

    public bool ProcessHover(int x, int y)
    {
        int newHover = HitTest(x, y);
        int newCheckbox = (newHover >= 0 && x >= ListX && x < ListX + CheckboxCols) ? newHover : -1;
        if (newHover == _hoveredRow && newCheckbox == _hoveredCheckboxRow) return false;
        _hoveredRow = newHover;
        _hoveredCheckboxRow = newCheckbox;
        return true;
    }

    /// <summary>
    /// Handles a click in the routine list. Clicking the checkbox cell toggles the lock; clicking the
    /// name selects the routine (and refocuses the minimap). Returns true when something changed.
    /// </summary>
    public bool ProcessClick(int x, int y, Protagonist protagonist)
    {
        int idx = HitTestIndex(x, y);
        if (idx < 0 || idx >= protagonist.RecordedRoutines.Count) return false;

        bool onCheckbox = x >= ListX && x < ListX + CheckboxCols;
        if (onCheckbox)
        {
            protagonist.RecordedRoutines[idx].Locked = !protagonist.RecordedRoutines[idx].Locked;
            return true;
        }

        if (_selectedIndex != idx)
        {
            _selectedIndex = idx;
            FireFocus(protagonist, force: false);
            return true;
        }
        return false;
    }

    private void FireFocus(Protagonist protagonist, bool force)
    {
        if (_selectedIndex < 0 || _selectedIndex >= protagonist.RecordedRoutines.Count) return;
        int locId = protagonist.RecordedRoutines[_selectedIndex].LocationId;
        if (!force && locId == _lastFocusedLocationId) return;
        _lastFocusedLocationId = locId;
        OnRoutineFocused?.Invoke(locId);
    }

    private int HitTest(int x, int y)
    {
        if (x < ListX || x >= ListX + ListWidth) return -1;
        foreach (var (row, _) in _hitRows)
            if (y == row) return row;
        return -1;
    }

    private int HitTestIndex(int x, int y)
    {
        if (x < ListX || x >= ListX + ListWidth) return -1;
        foreach (var (row, routineIndex) in _hitRows)
            if (y == row) return routineIndex;
        return -1;
    }

    // ═══════════════════════════════════════════════════════════════
    // Drawing helpers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Draws text in the center pane, truncated to the pane width.</summary>
    private void CenterText(int x, int y, string text, Vector4 fg)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (text.Length > CenterWidth) text = text.Substring(0, CenterWidth - 1) + "…";
        _terminal.Text(x, y, text, fg, Config.Colors.Black);
    }
}
