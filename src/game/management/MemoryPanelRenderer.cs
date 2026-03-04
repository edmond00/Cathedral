using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Management;

/// <summary>
/// Renders the Memory tab of the management screen.
/// Uses the full width cols 17-99 (no separator, no body art).
///
/// Rows 2        : title
/// Rows 4+       : five memory modules (header + slot grid)
/// Row ~76+      : inline detail panel for hovered skill
/// </summary>
public class MemoryPanelRenderer
{
    private readonly TerminalHUD _terminal;

    // ── Layout constants ─────────────────────────────────────────
    private const int StartX          = 17;
    private const int EndX            = 99;
    private const int AreaWidth       = EndX - StartX + 1; // 83
    private const int TitleRow        = 2;
    private const int ModulesStartRow = 4;
    private const int DetailPanelRow  = 77;
    private const int SlotWidth       = 20;
    private const int SlotHeight      = 3;
    private const int SlotsPerRow     = 4;

    // ── Narrow 3-column layout (Sensory | Procedural | Semantic) ─
    // 83 = 27 + │ + 27 + │ + 27
    private const int ColW            = 27;
    private const int ColSlotW        = 13; // 2 slots × 13 = 26 ≤ 27
    private const int ColSlotsPerRow  = 2;
    private const int Div1X           = StartX + ColW;          // 44
    private const int Div2X           = StartX + 2 * ColW + 1;  // 72
    private static readonly int[] ColStartX = { StartX, Div1X + 1, Div2X + 1 }; // 17, 45, 73

    // ── Area and slot backgrounds ────────────────────────────────
    private static readonly Vector4 AreaBg      = new(0.04f, 0.04f, 0.04f, 1.0f);
    private static readonly Vector4 HeaderBg    = new(0.11f, 0.11f, 0.11f, 1.0f);
    private static readonly Vector4 FilledBg    = new(0.09f, 0.09f, 0.09f, 1.0f);
    private static readonly Vector4 FilledBgHov = new(0.15f, 0.15f, 0.10f, 1.0f);
    private static readonly Vector4 EmptyBg     = new(0.06f, 0.06f, 0.06f, 1f);
    private static readonly Vector4 DetailBg    = new(0.08f, 0.08f, 0.08f, 1f);
    private static readonly Vector4 DetailTitle = new(0.12f, 0.12f, 0.08f, 1f);

    // ── Unified slot colors (same for every module) ─────────────
    private static readonly Vector4 TitleColor  = Config.Colors.White;
    private static readonly Vector4 SlotBorder  = Config.Colors.MediumGray60;
    private static readonly Vector4 SlotText    = Config.Colors.LightGray75;
    private static readonly Vector4 SubtitleCol = Config.Colors.DarkGray40;
    private static readonly Vector4 SepColor    = Config.Colors.DarkGray35;
    private static readonly Vector4 EmptyBorder = Config.Colors.DarkGray35;
    private static readonly Vector4 EmptyText   = new(0.22f, 0.22f, 0.22f, 1f);
    private static readonly Vector4 BlockedFill = Config.Colors.DarkGray20;

    // ── State ────────────────────────────────────────────────────
    private PartyMember? _member;

    private record struct SlotHit(MemoryModuleType Module, int SlotIndex,
                                  int X0, int Y0, int X1, int Y1);
    private readonly List<SlotHit> _slotHits = new();
    private (MemoryModuleType mod, int slot)? _hoveredSlot = null;

    // ── Constructor ──────────────────────────────────────────────
    /// <param name="popup">Ignored — detail is shown inline. Kept for API compat.</param>
    public MemoryPanelRenderer(TerminalHUD terminal, PopupTerminalHUD? popup = null)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }

    // ═══════════════════════════════════════════════════════════════
    // Public API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Full render of the memory panel for a party member.</summary>
    public void Render(PartyMember member)
    {
        _member = member;
        _slotHits.Clear();

        // Background (memory area cols 17-99, no right separator)
        for (int y = 0; y < 100; y++)
            for (int x = StartX; x <= EndX; x++)
                _terminal.SetCell(x, y, ' ', Config.Colors.Black, AreaBg);

        // Page title
        const string title = "M E M O R Y";
        int titleX = StartX + (AreaWidth - title.Length) / 2;
        _terminal.Text(titleX, TitleRow, title, Config.Colors.BrightYellow, AreaBg);

        if (member.MemoryModules.Count == 0)
        {
            _terminal.Text(StartX + 2, TitleRow + 2,
                "No memory modules — call InitializeMemory() first.",
                SepColor, AreaBg);
            RenderDetailPanel();
            return;
        }

        int row = ModulesStartRow;

        // ── Section 1: Working Memory (full width) ───────────────
        var working = member.MemoryModules.FirstOrDefault(
            m => m.Type == MemoryModuleType.Working);
        if (working != null)
        {
            row = RenderModuleFull(working, row);
            row += 2;
        }

        // ── Section 2: Sensory | Procedural | Semantic (3 columns) ─
        var col0 = member.MemoryModules.FirstOrDefault(
            m => m.Type == MemoryModuleType.Sensory);
        var col1 = member.MemoryModules.FirstOrDefault(
            m => m.Type == MemoryModuleType.Procedural);
        var col2 = member.MemoryModules.FirstOrDefault(
            m => m.Type == MemoryModuleType.Semantic);
        if (col0 != null || col1 != null || col2 != null)
        {
            row = RenderThreeColumns(row, col0, col1, col2);
            row += 2;
        }

        // ── Section 3: Residual Memory (full width) ──────────────
        var residual = member.MemoryModules.FirstOrDefault(
            m => m.Type == MemoryModuleType.Residual);
        if (residual != null)
            row = RenderModuleFull(residual, row);

        RenderDetailPanel();
    }

    /// <summary>Handle mouse hover — returns true if the hover state changed (triggers re-render).</summary>
    public bool ProcessHover(int x, int y)
    {
        (MemoryModuleType, int)? newSlot = null;
        foreach (var hit in _slotHits)
        {
            if (x >= hit.X0 && x <= hit.X1 && y >= hit.Y0 && y <= hit.Y1)
            {
                newSlot = (hit.Module, hit.SlotIndex);
                break;
            }
        }

        if (newSlot == _hoveredSlot) return false;
        _hoveredSlot = newSlot;
        return true;
    }

    /// <summary>Clear hover state.</summary>
    public void ClearHover()
    {
        _hoveredSlot = null;
    }

    // ═══════════════════════════════════════════════════════════════
    // Module rendering
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Render one memory module full-width. Returns next free row.</summary>
    private int RenderModuleFull(MemoryModule module, int startRow)
    {
        string label    = GetModuleLabel(module.Type);
        string subtitle = GetModuleSubtitle(module.Type);
        string badge    = $"{module.FilledCount} / {module.Capacity}";

        // Thin separator line before every module
        for (int x = StartX; x <= EndX; x++)
            _terminal.SetCell(x, startRow, '─', SepColor, AreaBg);
        startRow++;

        // Header row (elevated bg, full width)
        for (int x = StartX; x <= EndX; x++)
            _terminal.SetCell(x, startRow, ' ', TitleColor, HeaderBg);

        // "  LABEL  · subtitle" left-aligned
        string headerLeft = $"  {label}";
        _terminal.Text(StartX, startRow, headerLeft, TitleColor, HeaderBg);
        if (subtitle.Length > 0)
        {
            int subX = StartX + headerLeft.Length + 1;
            _terminal.Text(subX, startRow, $"· {subtitle}", SubtitleCol, HeaderBg);
        }

        // Badge right-aligned
        _terminal.Text(EndX - badge.Length, startRow, badge,
            Config.Colors.DarkYellowGrey, HeaderBg);
        startRow++;

        // Slot grid
        int row       = startRow;
        int slotIndex = 0;
        while (slotIndex < module.Capacity)
        {
            int inRow = Math.Min(SlotsPerRow, module.Capacity - slotIndex);
            for (int col = 0; col < inRow; col++)
            {
                int sx = StartX + col * SlotWidth;
                bool isHovered = _hoveredSlot.HasValue
                    && _hoveredSlot.Value.mod  == module.Type
                    && _hoveredSlot.Value.slot == slotIndex + col;
                RenderSlot(module.Slots[slotIndex + col], module.Type,
                           slotIndex + col, sx, row, isHovered);
            }
            slotIndex += SlotsPerRow;
            row       += SlotHeight;
        }
        return row;
    }

    private void RenderSlot(MemorySlot slot, MemoryModuleType moduleType, int slotIndex,
                             int x, int y, bool isHovered, int slotW = SlotWidth)
    {
        _slotHits.Add(new SlotHit(moduleType, slotIndex,
            x, y, x + slotW - 1, y + SlotHeight - 1));

        if (slot.IsBlocked)
        {
            _terminal.DrawBox(x, y, slotW, SlotHeight, BoxStyle.Single, BlockedFill, AreaBg);
            _terminal.Text(x + 1, y + 1, new string('░', slotW - 2), BlockedFill, AreaBg);
            return;
        }

        if (!slot.IsFilled)
        {
            _terminal.DrawBox(x, y, slotW, SlotHeight, BoxStyle.Single, EmptyBorder, EmptyBg);
            for (int ix = x + 1; ix < x + slotW - 1; ix++)
                _terminal.SetCell(ix, y + 1, ' ', EmptyText, EmptyBg);
            string emptyLabel = slotW >= 11 ? "· empty ·" : "·";
            int lx = x + 1 + Math.Max(0, (slotW - 2 - emptyLabel.Length) / 2);
            _terminal.Text(lx, y + 1, emptyLabel, EmptyText, EmptyBg);
            return;
        }

        // Filled slot
        Vector4 border  = isHovered ? Config.Colors.BrightYellow : SlotBorder;
        Vector4 textCol = isHovered ? Config.Colors.BrightYellow : SlotText;
        Vector4 slotBg  = isHovered ? FilledBgHov : FilledBg;

        _terminal.DrawBox(x, y, slotW, SlotHeight, BoxStyle.Single, border, slotBg);
        for (int ix = x + 1; ix < x + slotW - 1; ix++)
            _terminal.SetCell(ix, y + 1, ' ', textCol, slotBg);

        string lvl      = $"L{slot.Skill!.Level}";
        int    maxNameW = slotW - 2 - lvl.Length - 1;
        string name     = slot.Skill.DisplayName;
        if (name.Length > maxNameW) name = name.Length > 0 ? name[..Math.Max(0, maxNameW)] : "";

        _terminal.Text(x + 1, y + 1, name, textCol, slotBg);
        _terminal.Text(x + slotW - 1 - lvl.Length, y + 1, lvl,
            isHovered ? Config.Colors.BrightYellow : Config.Colors.GoldYellow, slotBg);
    }

    // ═══════════════════════════════════════════════════════════════
    // Three-column section
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders Sensory | Procedural | Semantic side-by-side with vertical dividers.
    /// Returns the row after the tallest column.
    /// </summary>
    private int RenderThreeColumns(int startRow,
        MemoryModule? m0, MemoryModule? m1, MemoryModule? m2)
    {
        MemoryModule?[] mods = { m0, m1, m2 };

        // Full-width separator before the tri-panel
        for (int x = StartX; x <= EndX; x++)
            _terminal.SetCell(x, startRow, '─', SepColor, AreaBg);
        startRow++;

        // Calculate each column's height so we can draw dividers full-height
        static int ColHeight(MemoryModule? m) =>
            m == null ? 1
            : 1 + (int)Math.Ceiling(m.Capacity / (double)ColSlotsPerRow) * SlotHeight;
        int maxH = Math.Max(ColHeight(m0), Math.Max(ColHeight(m1), ColHeight(m2)));
        int endRow = startRow + maxH;

        // Draw vertical dividers across the whole section height
        for (int y = startRow; y < endRow; y++)
        {
            _terminal.SetCell(Div1X, y, '│', SepColor, AreaBg);
            _terminal.SetCell(Div2X, y, '│', SepColor, AreaBg);
        }
        // T-junctions at the separator row above
        _terminal.SetCell(Div1X, startRow - 1, '┬', SepColor, AreaBg);
        _terminal.SetCell(Div2X, startRow - 1, '┬', SepColor, AreaBg);

        // Render each module in its column
        for (int i = 0; i < 3; i++)
        {
            if (mods[i] != null)
                RenderColumnModule(mods[i]!, startRow, ColStartX[i]);
        }

        return endRow;
    }

    /// <summary>Renders one module inside a narrow column. Does NOT draw a separator (already done by caller).</summary>
    private void RenderColumnModule(MemoryModule module, int startRow, int colX)
    {
        int colEndX    = colX + ColW - 1;
        string label   = GetModuleLabel(module.Type);
        string badge   = $"{module.FilledCount}/{module.Capacity}";

        // Truncate label to fit column
        int maxLabelW = ColW - badge.Length - 3;
        if (label.Length > maxLabelW) label = label[..maxLabelW];

        // Header row
        for (int x = colX; x <= colEndX; x++)
            _terminal.SetCell(x, startRow, ' ', TitleColor, HeaderBg);
        _terminal.Text(colX + 1, startRow, label, TitleColor, HeaderBg);
        _terminal.Text(colEndX - badge.Length, startRow, badge,
            Config.Colors.DarkYellowGrey, HeaderBg);

        // Slot grid
        int row       = startRow + 1;
        int slotIndex = 0;
        while (slotIndex < module.Capacity)
        {
            int inRow = Math.Min(ColSlotsPerRow, module.Capacity - slotIndex);
            for (int col = 0; col < inRow; col++)
            {
                int sx = colX + col * ColSlotW;
                bool isHovered = _hoveredSlot.HasValue
                    && _hoveredSlot.Value.mod  == module.Type
                    && _hoveredSlot.Value.slot == slotIndex + col;
                RenderSlot(module.Slots[slotIndex + col], module.Type,
                           slotIndex + col, sx, row, isHovered, ColSlotW);
            }
            slotIndex += ColSlotsPerRow;
            row       += SlotHeight;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Inline detail panel
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders skill details at the bottom of the memory area.
    /// Shows a hint when nothing is hovered; shows full skill info when hovering a filled slot.
    /// This replaces the fragile floating popup with a stable inline panel.
    /// </summary>
    private void RenderDetailPanel()
    {
        const int panelH = 14;

        // ── Separator above detail panel ─────────────────────────
        for (int x = StartX; x <= EndX; x++)
            _terminal.SetCell(x, DetailPanelRow - 1, '─', Config.Colors.DarkGray35, AreaBg);

        // Determine hovered skill
        Skill? skill = null;

        if (_hoveredSlot.HasValue && _member != null)
        {
            var (modType, slotIdx) = _hoveredSlot.Value;
            var mod = _member.MemoryModules.FirstOrDefault(m => m.Type == modType);
            if (mod != null && slotIdx < mod.Slots.Count && mod.Slots[slotIdx].IsFilled)
                skill = mod.Slots[slotIdx].Skill;
        }

        if (skill == null)
        {
            for (int y = DetailPanelRow; y < DetailPanelRow + panelH; y++)
                for (int x = StartX; x <= EndX; x++)
                    _terminal.SetCell(x, y, ' ', Config.Colors.Black, AreaBg);
            _terminal.Text(StartX + 2, DetailPanelRow + 1,
                "Hover a filled memory slot to inspect the skill.",
                SubtitleCol, AreaBg);
            return;
        }

        // Detail background
        for (int y = DetailPanelRow; y < DetailPanelRow + panelH; y++)
            for (int x = StartX; x <= EndX; x++)
                _terminal.SetCell(x, y, ' ', Config.Colors.Black, DetailBg);

        int row = DetailPanelRow;

        // Title row (skill name + level) — uniform bright highlight, no per-module accent
        for (int x = StartX; x <= EndX; x++)
            _terminal.SetCell(x, row, ' ', TitleColor, DetailTitle);
        _terminal.Text(StartX + 2, row, skill.DisplayName, Config.Colors.BrightYellow, DetailTitle);
        string lvlStr = $"Level {skill.Level}";
        _terminal.Text(EndX - lvlStr.Length, row, lvlStr,
            Config.Colors.GoldYellow, DetailTitle);
        row++;

        // ── Separator ─────────────────────────────────────────────
        for (int x = StartX; x <= EndX; x++)
            _terminal.SetCell(x, row, '─', Config.Colors.DarkGray35, DetailBg);
        row++;

        // ── Two-column layout: meta (left) | description (right) ──
        const int leftW  = 30;
        const int rightX = StartX + leftW + 2;
        const int rightW = AreaWidth - leftW - 3;

        var metaLines = new (string label, string value, Vector4 valCol)[]
        {
            ("Memory type",
             skill.MemoryType.ToString(),
             SlotText),
            ("Functions",
             string.Join(", ", skill.Functions),
             Config.Colors.LightGray75),
            ("Primary organ",
             skill.Organs.Length > 0 ? skill.Organs[0] : "—",
             Config.Colors.LightGray75),
            ("Organ score",
             _member != null ? _member.GetOrganScoreForSkill(skill).ToString() : "—",
             Config.Colors.LightGray75),
        };

        int metaRow = row;
        foreach (var (lbl, val, vc) in metaLines)
        {
            _terminal.Text(StartX + 2, metaRow, lbl, Config.Colors.DarkGray40, DetailBg);
            _terminal.Text(StartX + 2 + lbl.Length + 1, metaRow, val, vc, DetailBg);
            metaRow++;
        }

        // Vertical divider between columns
        for (int y = row; y < DetailPanelRow + panelH - 1; y++)
            _terminal.SetCell(StartX + leftW, y, '│', Config.Colors.DarkGray35, DetailBg);

        // Description with proper word-wrap
        string desc  = skill.ShortDescription ?? "(no description)";
        var    words = desc.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string line  = "";
        int    descRow = row;
        foreach (var word in words)
        {
            if (line.Length + word.Length + (line.Length > 0 ? 1 : 0) > rightW)
            {
                if (descRow < DetailPanelRow + panelH - 1)
                    _terminal.Text(rightX, descRow++, line, Config.Colors.LightGray75, DetailBg);
                line = word;
            }
            else
                line = line.Length == 0 ? word : line + " " + word;
        }
        if (line.Length > 0 && descRow < DetailPanelRow + panelH - 1)
            _terminal.Text(rightX, descRow, line, Config.Colors.LightGray75, DetailBg);
    }

    // ═══════════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════════

    private static Vector4 AccentBg(Vector4 _) => FilledBg;      // legacy shim
    private static Vector4 AccentBgHover(Vector4 _) => FilledBgHov; // legacy shim

    private static Vector4 GetModuleColor(MemoryModuleType _) => SlotBorder; // kept for compat, unused

    private static string GetModuleSubtitle(MemoryModuleType type) => type switch
    {
        MemoryModuleType.Working    => "any skill  ▶  short-term",
        MemoryModuleType.Procedural => "motor · physical skills",
        MemoryModuleType.Semantic   => "conceptual · factual skills",
        MemoryModuleType.Sensory    => "perceptual · experiential",
        MemoryModuleType.Residual   => "forgetting queue  ▶ FIFO ▶",
        _                           => ""
    };

    private static string GetModuleLabel(MemoryModuleType type) => type switch
    {
        MemoryModuleType.Working    => "WORKING MEMORY",
        MemoryModuleType.Procedural => "PROCEDURAL MEMORY",
        MemoryModuleType.Semantic   => "SEMANTIC MEMORY",
        MemoryModuleType.Sensory    => "SENSORY MEMORY",
        MemoryModuleType.Residual   => "RESIDUAL MEMORY",
        _                           => type.ToString().ToUpper()
    };
}
