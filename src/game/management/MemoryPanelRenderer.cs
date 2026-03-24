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
/// Row ~76+      : inline detail panel for hovered modusMentis
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
    private static readonly Vector4 SelectedBg  = new(0.20f, 0.18f, 0.06f, 1.0f);
    private static readonly Vector4 EmptyBg     = new(0.06f, 0.06f, 0.06f, 1f);
    private static readonly Vector4 UnusableBg  = new(0.04f, 0.04f, 0.04f, 1f); // same as AreaBg — slots blend in
    private static readonly Vector4 UnusableFg  = new(0.10f, 0.10f, 0.10f, 1f); // barely visible border
    private static readonly Vector4 DetailBg    = new(0.08f, 0.08f, 0.08f, 1f);
    private static readonly Vector4 DetailTitle = new(0.12f, 0.12f, 0.08f, 1f);
    private static readonly Vector4 BtnHovBg    = new(0.16f, 0.16f, 0.10f, 1f);

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
    private record struct ButtonHit(string Id, int X0, int Y0, int X1, int Y1, bool Enabled);
    private readonly List<SlotHit>   _slotHits   = new();
    private readonly List<ButtonHit> _buttonHits = new();
    private (MemoryModuleType mod, int slot)? _hoveredSlot   = null;
    private (MemoryModuleType mod, int slot)? _selectedSlot  = null;
    private string?                           _hoveredButton = null;

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
        _buttonHits.Clear();

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

            // Extend the vertical dividers through the 2-row gap below the tri-panel
            for (int y = row; y < row + 2; y++)
            {
                _terminal.SetCell(Div1X, y, '│', SepColor, AreaBg);
                _terminal.SetCell(Div2X, y, '│', SepColor, AreaBg);
            }
            row += 2;
        }

        // ── Section 3: Residual Memory (full width) ──────────────
        var residual = member.MemoryModules.FirstOrDefault(
            m => m.Type == MemoryModuleType.Residual);
        if (residual != null)
        {
            // Record where the separator will be drawn (RenderModuleFull draws it at startRow)
            int residualSepRow = row;
            row = RenderModuleFull(residual, row);
            // Overwrite the two divider positions on the separator with ┴ junctions
            _terminal.SetCell(Div1X, residualSepRow, '┴', SepColor, AreaBg);
            _terminal.SetCell(Div2X, residualSepRow, '┴', SepColor, AreaBg);
        }


        RenderDetailPanel();
    }

    /// <summary>Handle mouse hover — returns true if the hover state changed (triggers re-render).</summary>
    public bool ProcessHover(int x, int y)
    {
        bool changed = false;

        // Slot hover
        (MemoryModuleType, int)? newSlot = null;
        foreach (var hit in _slotHits)
        {
            if (x >= hit.X0 && x <= hit.X1 && y >= hit.Y0 && y <= hit.Y1)
            { newSlot = (hit.Module, hit.SlotIndex); break; }
        }
        if (newSlot != _hoveredSlot) { _hoveredSlot = newSlot; changed = true; }

        // Button hover
        string? newBtn = null;
        foreach (var btn in _buttonHits)
        {
            if (x >= btn.X0 && x <= btn.X1 && y >= btn.Y0 && y <= btn.Y1)
            { newBtn = btn.Id; break; }
        }
        if (newBtn != _hoveredButton) { _hoveredButton = newBtn; changed = true; }

        return changed;
    }

    /// <summary>Clear all hover and selection state (called when leaving the Memory tab).</summary>
    public void ClearHover()
    {
        _hoveredSlot   = null;
        _hoveredButton = null;
        _selectedSlot  = null;
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

        // Slot grid — iterate ALL slots (active + unusable)
        int row       = startRow;
        int slotIndex = 0;
        while (slotIndex < module.Slots.Count)
        {
            int inRow = Math.Min(SlotsPerRow, module.Slots.Count - slotIndex);
            for (int col = 0; col < inRow; col++)
            {
                int sx = StartX + col * SlotWidth;
                bool isHovered = _hoveredSlot.HasValue
                    && _hoveredSlot.Value.mod  == module.Type
                    && _hoveredSlot.Value.slot == slotIndex + col;
                bool isSelected = _selectedSlot.HasValue
                    && _selectedSlot.Value.mod  == module.Type
                    && _selectedSlot.Value.slot == slotIndex + col;
                RenderSlot(module.Slots[slotIndex + col], module.Type,
                           slotIndex + col, sx, row, isHovered, isSelected);
            }
            slotIndex += SlotsPerRow;
            row       += SlotHeight;
        }
        return row;
    }

    private void RenderSlot(MemorySlot slot, MemoryModuleType moduleType, int slotIndex,
                             int x, int y, bool isHovered, bool isSelected, int slotW = SlotWidth)
    {
        _slotHits.Add(new SlotHit(moduleType, slotIndex,
            x, y, x + slotW - 1, y + SlotHeight - 1));

        // Unusable slot (beyond active capacity) — barely visible, greyed out
        if (slot.IsUnusable)
        {
            _terminal.DrawBox(x, y, slotW, SlotHeight, BoxStyle.Single, UnusableFg, UnusableBg);
            int mid = x + 1 + (slotW - 2) / 2;
            _terminal.SetCell(mid, y + 1, '•', UnusableFg, UnusableBg);
            return;
        }

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

        // Filled slot — selected takes visual priority over hovered
        Vector4 border  = (isSelected || isHovered) ? Config.Colors.BrightYellow : SlotBorder;
        Vector4 textCol = (isSelected || isHovered) ? Config.Colors.BrightYellow : SlotText;
        Vector4 slotBg  = isSelected ? SelectedBg : (isHovered ? FilledBgHov : FilledBg);

        _terminal.DrawBox(x, y, slotW, SlotHeight, BoxStyle.Single, border, slotBg);
        for (int ix = x + 1; ix < x + slotW - 1; ix++)
            _terminal.SetCell(ix, y + 1, ' ', textCol, slotBg);

        string lvl      = $"L{slot.ModusMentis!.Level}";
        int    maxNameW = slotW - 2 - lvl.Length - 1;
        string name     = slot.ModusMentis.DisplayName;
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
            : 1 + (int)Math.Ceiling(m.Slots.Count / (double)ColSlotsPerRow) * SlotHeight;
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

        // Slot grid — iterate ALL slots (active + unusable)
        int row       = startRow + 1;
        int slotIndex = 0;
        while (slotIndex < module.Slots.Count)
        {
            int inRow = Math.Min(ColSlotsPerRow, module.Slots.Count - slotIndex);
            for (int col = 0; col < inRow; col++)
            {
                int sx = colX + col * ColSlotW;
                bool isHovered = _hoveredSlot.HasValue
                    && _hoveredSlot.Value.mod  == module.Type
                    && _hoveredSlot.Value.slot == slotIndex + col;
                bool isSelected = _selectedSlot.HasValue
                    && _selectedSlot.Value.mod  == module.Type
                    && _selectedSlot.Value.slot == slotIndex + col;
                RenderSlot(module.Slots[slotIndex + col], module.Type,
                           slotIndex + col, sx, row, isHovered, isSelected, ColSlotW);
            }
            slotIndex += ColSlotsPerRow;
            row       += SlotHeight;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Inline detail panel
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders modusMentis details at the bottom of the memory area.
    /// </summary>
    private void RenderDetailPanel()
    {
        const int panelH = 14;

        // Separator above detail panel
        for (int x = StartX; x <= EndX; x++)
            _terminal.SetCell(x, DetailPanelRow - 1, '─', Config.Colors.DarkGray35, AreaBg);

        // Determine selected modusMentis and its source module type
        ModusMentis? modusMentis = null;
        MemoryModuleType selectedModType = MemoryModuleType.Working;
        if (_selectedSlot.HasValue && _member != null)
        {
            var (modType, slotIdx) = _selectedSlot.Value;
            selectedModType = modType;
            var mod = _member.MemoryModules.FirstOrDefault(m => m.Type == modType);
            if (mod != null && slotIdx < mod.Slots.Count && mod.Slots[slotIdx].IsFilled)
                modusMentis = mod.Slots[slotIdx].ModusMentis;
        }

        if (modusMentis == null)
        {
            for (int y = DetailPanelRow; y < DetailPanelRow + panelH; y++)
                for (int x = StartX; x <= EndX; x++)
                    _terminal.SetCell(x, y, ' ', Config.Colors.Black, AreaBg);
            _terminal.Text(StartX + 2, DetailPanelRow + 1,
                "Click a filled memory slot to inspect and manage it.",
                SubtitleCol, AreaBg);
            return;
        }

        // Detail background
        for (int y = DetailPanelRow; y < DetailPanelRow + panelH; y++)
            for (int x = StartX; x <= EndX; x++)
                _terminal.SetCell(x, y, ' ', Config.Colors.Black, DetailBg);

        int row = DetailPanelRow;

        // Title row
        for (int x = StartX; x <= EndX; x++)
            _terminal.SetCell(x, row, ' ', TitleColor, DetailTitle);
        _terminal.Text(StartX + 2, row, modusMentis.DisplayName, Config.Colors.BrightYellow, DetailTitle);
        string lvlStr = $"Level {modusMentis.Level}";
        _terminal.Text(EndX - lvlStr.Length, row, lvlStr, Config.Colors.GoldYellow, DetailTitle);
        row++;

        // Separator
        for (int x = StartX; x <= EndX; x++)
            _terminal.SetCell(x, row, '─', Config.Colors.DarkGray35, DetailBg);
        row++;

        // Two-column layout
        const int leftW  = 46;
        const int rightX = StartX + leftW + 2;
        const int rightW = AreaWidth - leftW - 3;

        // Meta lines (left column, rows 2-5)
        var metaLines = new (string label, string value, Vector4 valCol)[]
        {
            ("Memory type",   modusMentis.MemoryType.ToString(),                              SlotText),
            ("Functions",     string.Join(", ", modusMentis.Functions),                       Config.Colors.LightGray75),
            ("Primary organ", modusMentis.Organs.Length > 0 ? modusMentis.Organs[0] : "—",         Config.Colors.LightGray75),
            ("Organ score",   _member != null ? _member.GetOrganScoreForModusMentis(modusMentis).ToString() : "—",
             Config.Colors.LightGray75),
        };
        int metaRow = row;
        foreach (var (lbl, val, vc) in metaLines)
        {
            _terminal.Text(StartX + 2, metaRow, lbl, Config.Colors.DarkGray40, DetailBg);
            _terminal.Text(StartX + 2 + lbl.Length + 1, metaRow, val, vc, DetailBg);
            metaRow++;
        }

        // Vertical divider
        for (int y = row; y < DetailPanelRow + panelH - 1; y++)
            _terminal.SetCell(StartX + leftW, y, '│', Config.Colors.DarkGray35, DetailBg);

        // Right column: description word-wrap
        string desc  = modusMentis.ShortDescription ?? "(no description)";
        var    words = desc.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string dline = "";
        int descRow  = row;
        foreach (var word in words)
        {
            if (dline.Length + word.Length + (dline.Length > 0 ? 1 : 0) > rightW)
            {
                if (descRow < DetailPanelRow + panelH - 1)
                    _terminal.Text(rightX, descRow++, dline, Config.Colors.LightGray75, DetailBg);
                dline = word;
            }
            else
                dline = dline.Length == 0 ? word : dline + " " + word;
        }
        if (dline.Length > 0 && descRow < DetailPanelRow + panelH - 1)
            _terminal.Text(rightX, descRow, dline, Config.Colors.LightGray75, DetailBg);

        // Action button (left column, row after meta + 1 gap)
        int btnRow = row + metaLines.Length + 1;
        if (btnRow < DetailPanelRow + panelH - 2)
        {
            if (selectedModType == MemoryModuleType.Working)
            {
                // CONSOLIDATE: move modusMentis to its typed long-term module
                var targetModType = modusMentis.MemoryType switch
                {
                    ModusMentisMemoryType.Procedural => MemoryModuleType.Procedural,
                    ModusMentisMemoryType.Sensory    => MemoryModuleType.Sensory,
                    _                          => MemoryModuleType.Semantic
                };
                var targetMod    = _member!.MemoryModules.FirstOrDefault(m => m.Type == targetModType);
                bool hasFreeSlot = targetMod != null
                    && targetMod.Slots.Any(s => !s.IsUnusable && !s.IsBlocked && !s.IsFilled);
                string? reason = hasFreeSlot ? null
                    : $"no free slot in {GetModuleLabel(targetModType).ToLower()}";
                RenderButton("consolidate", "CONSOLIDATE", StartX + 2, btnRow, hasFreeSlot, reason);
            }
            else if (selectedModType == MemoryModuleType.Procedural
                  || selectedModType == MemoryModuleType.Semantic
                  || selectedModType == MemoryModuleType.Sensory)
            {
                // ARCHIVE: move modusMentis to front of residual FIFO queue
                var srcMod     = _member!.MemoryModules.FirstOrDefault(m => m.Type == selectedModType);
                bool canArchive = (srcMod?.FilledCount ?? 0) > 1;
                string? reason = canArchive ? null : "at least 1 modusMentis must remain";
                RenderButton("archive", "ARCHIVE", StartX + 2, btnRow, canArchive, reason);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Click handling & actions
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Handle a left click. Returns true when state changed and a re-render is needed.
    /// May mutate MemoryModules (consolidate / archive actions).
    /// </summary>
    public bool ProcessClick(int x, int y)
    {
        // Clicks inside the detail panel area never deselect the current slot
        // (so the user can read/click buttons freely without losing selection).
        bool inDetailPanel = y >= DetailPanelRow - 1;

        // Buttons take priority
        foreach (var btn in _buttonHits)
        {
            if (x >= btn.X0 && x <= btn.X1 && y >= btn.Y0 && y <= btn.Y1)
            {
                if (!btn.Enabled) return false;
                switch (btn.Id)
                {
                    case "consolidate": ExecuteConsolidate(); break;
                    case "archive":     ExecuteArchive();     break;
                }
                return true;
            }
        }

        // Slot clicks
        foreach (var hit in _slotHits)
        {
            if (x >= hit.X0 && x <= hit.X1 && y >= hit.Y0 && y <= hit.Y1)
            {
                var mod = _member?.MemoryModules.FirstOrDefault(m => m.Type == hit.Module);
                bool isFilled = mod != null
                    && hit.SlotIndex < mod.Slots.Count
                    && mod.Slots[hit.SlotIndex].IsFilled
                    && !mod.Slots[hit.SlotIndex].IsUnusable;

                if (isFilled)
                {
                    var key = (hit.Module, hit.SlotIndex);
                    _selectedSlot = (_selectedSlot == key) ? null : key;
                }
                else if (_selectedSlot.HasValue)
                    _selectedSlot = null;
                else
                    return false;

                return true;
            }
        }

        // Click outside slots and buttons
        if (!inDetailPanel && _selectedSlot.HasValue)
        {
            _selectedSlot = null;
            return true;
        }
        return false;
    }

    private void ExecuteConsolidate()
    {
        if (!_selectedSlot.HasValue || _member == null) return;
        var (modType, slotIdx) = _selectedSlot.Value;
        if (modType != MemoryModuleType.Working) return;

        var working = _member.MemoryModules.FirstOrDefault(m => m.Type == MemoryModuleType.Working);
        if (working == null || slotIdx >= working.Slots.Count) return;
        var srcSlot = working.Slots[slotIdx];
        if (!srcSlot.IsFilled) return;
        var modusMentis = srcSlot.ModusMentis!;

        var targetType = modusMentis.MemoryType switch
        {
            ModusMentisMemoryType.Procedural => MemoryModuleType.Procedural,
            ModusMentisMemoryType.Sensory    => MemoryModuleType.Sensory,
            _                          => MemoryModuleType.Semantic
        };
        var target = _member.MemoryModules.FirstOrDefault(m => m.Type == targetType);
        if (target == null || !target.TryAdd(modusMentis)) return;

        srcSlot.ModusMentis = null;
        _selectedSlot = null;
    }

    private void ExecuteArchive()
    {
        if (!_selectedSlot.HasValue || _member == null) return;
        var (modType, slotIdx) = _selectedSlot.Value;

        var source = _member.MemoryModules.FirstOrDefault(m => m.Type == modType);
        if (source == null || slotIdx >= source.Slots.Count) return;
        var srcSlot = source.Slots[slotIdx];
        if (!srcSlot.IsFilled) return;
        var modusMentis = srcSlot.ModusMentis!;

        var residual = _member.MemoryModules.FirstOrDefault(m => m.Type == MemoryModuleType.Residual);
        if (residual == null) return;

        srcSlot.ModusMentis = null;
        residual.Prepend(modusMentis);
        _selectedSlot = null;
    }

    /// <summary>Draws a labeled action button and registers its hit area.</summary>
    private void RenderButton(string id, string label, int x, int y, bool enabled, string? disabledReason)
    {
        string text   = $"[ {label} ]";
        bool   hov    = _hoveredButton == id && enabled;
        Vector4 fg    = enabled ? (hov ? Config.Colors.BrightYellow : Config.Colors.LightGray85)
                                : SubtitleCol;
        Vector4 bg    = hov ? BtnHovBg : DetailBg;

        for (int ix = x; ix < x + text.Length; ix++)
            _terminal.SetCell(ix, y, ' ', fg, bg);
        _terminal.Text(x, y, text, fg, bg);

        _buttonHits.Add(new ButtonHit(id, x, y, x + text.Length - 1, y, enabled));

        // Disabled reason on the next row
        if (!enabled && disabledReason != null && y + 1 < DetailPanelRow + 14 - 1)
            _terminal.Text(x + 2, y + 1, $"↳ {disabledReason}", EmptyText, DetailBg);
    }


    private static Vector4 AccentBg(Vector4 _) => FilledBg;      // legacy shim
    private static Vector4 AccentBgHover(Vector4 _) => FilledBgHov; // legacy shim

    private static Vector4 GetModuleColor(MemoryModuleType _) => SlotBorder; // kept for compat, unused

    private static string GetModuleSubtitle(MemoryModuleType type) => type switch
    {
        MemoryModuleType.Working    => "any modus mentis  ▶  short-term",
        MemoryModuleType.Procedural => "motor · physical modiMentis",
        MemoryModuleType.Semantic   => "conceptual · factual modiMentis",
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
