using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;
using Cathedral.Game.Narrative;
using Cathedral.Game.Creation;

namespace Cathedral.Game.Management;

/// <summary>
/// Renders the Inventory tab of the management screen.
///
/// Layout:
///   Cols  0-15 : Left panel (drawn by ManagementMenuRenderer)
///   Col   16   : Vertical separator
///   Cols 17-60 : Body art (ascii_art.txt rendered by BodyArtViewer), with item boxes
///                overlaid at each of the 13 anchor positions from gears.txt.
///   Col   61   : Vertical separator
///   Cols 62-99 : Right info panel (item detail / content list)
///
/// Per-anchor layout (top to bottom):
///   • 1-row header  : anchor label on a light bg / dark fg strip (always visible)
///   • item boxes    : one box per item, height = item.SlotCount rows
///   • placeholder(s): one 3-row dim box per 3 remaining capacity slots
///
/// Drag &amp; drop:
///   ProcessClick   (mouse-down) starts a drag when an item box is hit.
///   ProcessHover   (mouse-move) updates the hovered drop target; the right panel
///                  shows info about whatever item/anchor is currently hovered.
///   ProcessMouseUp (mouse-up) completes the drag (moves item) or cancels it when the
///                  pointer is still over the same source anchor.
/// </summary>
public sealed class InventoryMenuRenderer
{
    // ── Dependencies ──────────────────────────────────────────────
    private readonly TerminalHUD _terminal;
    private readonly BodyArtViewer _bodyViewer;
    private readonly GearAnchorData _gearData;
    private readonly PopupTerminalHUD? _popup;

    // ── Layout ────────────────────────────────────────────────────
    private const int ArtOffsetX    = 17;   // left edge of body art  (cols 17-56)
    private const int ArtWidth      = 40;   // body art character width
    private const int ItemBoxW      = 12;   // fixed width for all item boxes
    private const int PlaceholderH  =  3;   // height of one placeholder (= 1 slot chunk)

    // Right panel (mirrors BodyArtViewer constants)
    private const int RightPanelX   = BodyArtViewer.PanelX;         // 62
    private const int RightContentX = BodyArtViewer.PanelContentX;  // 64
    private const int RightContentW = BodyArtViewer.PanelContentW;  // 34
    private const int InfoStartRow  = 4;    // first usable row in right panel
    private const int InfoEndRow    = 90;   // last usable row in right panel

    // ── Colours ───────────────────────────────────────────────────
    private static readonly Vector4 BgColor      = new(0.00f, 0.00f, 0.00f, 1f);
    private static readonly Vector4 RightPanelBg = BgColor;

    // Anchor header (always visible, light bg / dark text)
    private static readonly Vector4 HeaderBg           = new(0.22f, 0.21f, 0.17f, 1f);
    private static readonly Vector4 HeaderFg           = new(0.85f, 0.82f, 0.70f, 1f);
    private static readonly Vector4 DropTargetHeaderBg = new(0.48f, 0.42f, 0.06f, 1f);
    private static readonly Vector4 DropTargetHeaderFg = new(0.98f, 0.92f, 0.20f, 1f);

    // Item box states
    private static readonly Vector4 EmptyBoxBorder  = Config.Colors.DarkGray35;
    private static readonly Vector4 EmptyBoxBg      = new(0.03f, 0.03f, 0.03f, 1f);
    private static readonly Vector4 EmptyTextColor  = new(0.22f, 0.22f, 0.22f, 1f);

    private static readonly Vector4 NormalBoxBorder = Config.Colors.MediumGray60;
    private static readonly Vector4 NormalBoxBg     = new(0.06f, 0.06f, 0.06f, 1f);
    private static readonly Vector4 NormalTextColor = Config.Colors.LightGray75;

    private static readonly Vector4 HoveredBoxBorder = Config.Colors.LightGray75;
    private static readonly Vector4 HoveredBoxBg     = new(0.10f, 0.09f, 0.03f, 1f);
    private static readonly Vector4 HoveredTextColor = Config.Colors.White;

    private static readonly Vector4 SelectedBoxBorder = Config.Colors.BrightYellow;
    private static readonly Vector4 SelectedBoxBg     = new(0.14f, 0.12f, 0.02f, 1f);
    private static readonly Vector4 SelectedTextColor = Config.Colors.BrightYellow;

    private static readonly Vector4 DragBoxBorder  = Config.Colors.BrightYellow;
    private static readonly Vector4 DragBoxBg      = new(0.12f, 0.10f, 0.01f, 1f);
    private static readonly Vector4 DragTextColor  = Config.Colors.BrightYellow;

    private static readonly Vector4 DropTargetBorder = Config.Colors.BrightYellow;
    private static readonly Vector4 DropTargetBg     = new(0.18f, 0.14f, 0.00f, 1f);

    // Grayed-out colours for anchors that cannot accept the dragged item
    private static readonly Vector4 GrayHeaderBg    = new(0.10f, 0.10f, 0.10f, 1f);
    private static readonly Vector4 GrayHeaderFg    = new(0.30f, 0.30f, 0.30f, 1f);
    private static readonly Vector4 GrayBoxBorder   = new(0.18f, 0.18f, 0.18f, 1f);
    private static readonly Vector4 GrayBoxBg       = new(0.02f, 0.02f, 0.02f, 1f);
    private static readonly Vector4 GrayTextColor   = new(0.25f, 0.25f, 0.25f, 1f);

    // Right panel colours
    private static readonly Vector4 InfoTitleColor  = Config.Colors.BrightYellow;
    private static readonly Vector4 InfoLabelColor  = Config.Colors.MediumGray60;
    private static readonly Vector4 InfoValueColor  = Config.Colors.LightGray75;
    private static readonly Vector4 SepColor        = Config.Colors.DarkGray35;

    private static readonly Vector4 ContentNormal   = Config.Colors.MediumGray60;
    private static readonly Vector4 ContentHovered  = Config.Colors.White;
    private static readonly Vector4 ContentSelected = Config.Colors.BrightYellow;
    private static readonly Vector4 ContentBg       = BgColor;
    private static readonly Vector4 ContentSelBg    = new(0.14f, 0.12f, 0.02f, 1f);

    // ── Hit regions ───────────────────────────────────────────────
    // ItemIdx >= 0 : index in EquippedItems[Anchor] list
    // ItemIdx == -1: placeholder (drop zone, no item)
    private record struct ItemHit(EquipmentAnchor Anchor, int ItemIdx, int X0, int Y0, int X1, int Y1);
    private readonly List<ItemHit> _itemHits = new();

    private record struct ContentHit(int Index, int Y0, int Y1);
    private readonly List<ContentHit> _contentHits = new();

    // ── State ─────────────────────────────────────────────────────
    private EquipmentAnchor? _hoveredAnchor  = null;
    private int              _hoveredItemIdx = -1;
    private EquipmentAnchor? _selectedAnchor = null;
    private int              _selectedItemIdx = -1; // -1 = anchor header selected
    private int              _hoveredContent  = -1;
    private readonly List<int> _selectedContentPath = new();  // multi-level content selection path

    // Drag state
    private bool             _isDragging          = false;
    private Item?            _dragItem            = null;
    private EquipmentAnchor? _dragSourceAnchor    = null;
    private int              _dragSourceItemIdx   = -1;
    private ContainerItem?   _dragSourceContainer = null;  // non-null when dragged from container contents

    // Pending drag — mouse-down records intent; promoted to real drag
    // only after mouse movement or a short delay, so a quick click selects.
    private bool             _pendingDrag          = false;
    private Item?            _pendingDragItem      = null;
    private EquipmentAnchor? _pendingDragAnchor    = null;
    private int              _pendingDragItemIdx   = -1;
    private ContainerItem?   _pendingDragContainer = null;  // non-null when from content
    private int              _pendingClickX        = 0;
    private int              _pendingClickY        = 0;
    private long             _pendingClickTick     = 0;
    private const long       DragStartDelayMs      = 300;
    private const int        DragStartMovePx       = 2;

    // Drag hover info — "sticky": persists after cursor leaves the item box.
    // Used for body-art blink effect and as a fallback container drop target.
    private Item?            _dragHoverItem      = null;
    private EquipmentAnchor? _dragHoverAnchor    = null;
    private long             _dragHoverStartTick = 0;
    private bool             _dragHoverReady     = false;
    private const long       DragHoverDelayMs    = 1500;

    // Track whether mouse is over the right panel (for conditional drag preview).
    private bool             _hoveringRightPanel = false;

    // Blink state for the hovered item box during the delay period.
    private bool             _blinkOn            = false;
    private long             _blinkLastToggle    = 0;
    private const long       BlinkIntervalMs     = 300;

    // Blink colours (subtle pulse)
    private static readonly Vector4 BlinkOnBorder  = Config.Colors.LightGray75;
    private static readonly Vector4 BlinkOnBg      = new(0.08f, 0.07f, 0.02f, 1f);
    private static readonly Vector4 BlinkOnText    = Config.Colors.LightGray75;
    private static readonly Vector4 BlinkOffBorder = NormalBoxBorder;
    private static readonly Vector4 BlinkOffBg     = NormalBoxBg;
    private static readonly Vector4 BlinkOffText   = NormalTextColor;

    // Cached member reference for event handlers
    private PartyMember? _member;

    // ── Constructor ───────────────────────────────────────────────
    public InventoryMenuRenderer(TerminalHUD terminal, BodyArtViewer bodyViewer, GearAnchorData gearData,
                                  PopupTerminalHUD? popup = null)
    {
        _terminal   = terminal   ?? throw new ArgumentNullException(nameof(terminal));
        _bodyViewer = bodyViewer ?? throw new ArgumentNullException(nameof(bodyViewer));
        _gearData   = gearData   ?? throw new ArgumentNullException(nameof(gearData));
        _popup      = popup;
    }

    // ═══════════════════════════════════════════════════════════════
    // Public API
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Full render of the inventory view for <paramref name="member"/>.</summary>
    public void Render(PartyMember member)
    {
        _member = member;
        _itemHits.Clear();
        _contentHits.Clear();

        _bodyViewer.ClearHover();
        _bodyViewer.RenderBodyArt(brightness: 0.5f);

        for (int y = 0; y < 100; y++)
            _terminal.SetCell(RightPanelX - 1, y, '│', SepColor, BgColor);

        for (int y = 0; y < 100; y++)
            for (int x = RightPanelX; x < 100; x++)
                _terminal.SetCell(x, y, ' ', BgColor, RightPanelBg);

        foreach (EquipmentAnchor anchor in Enum.GetValues<EquipmentAnchor>())
            RenderAnchor(member, anchor);

        RenderRightPanel(member);
    }

    /// <summary>Process a mouse hover. Returns true when displayed state changed.</summary>
    public bool ProcessHover(int x, int y)
    {
        // Promote pending drag if the mouse moved enough.
        if (_pendingDrag && !_isDragging)
        {
            int dx = x - _pendingClickX;
            int dy = y - _pendingClickY;
            if (dx * dx + dy * dy >= DragStartMovePx * DragStartMovePx)
            {
                PromotePendingDrag();
                return true;
            }
        }

        var hit        = HitTestItem(x, y);
        var newAnchor  = hit?.Anchor;
        var newIdx     = hit?.ItemIdx ?? -1;
        var newContent = HitTestContent(y);
        bool onRight   = x >= RightPanelX;

        bool changed = newAnchor != _hoveredAnchor || newIdx != _hoveredItemIdx
                     || newContent != _hoveredContent || onRight != _hoveringRightPanel;
        _hoveredAnchor      = newAnchor;
        _hoveredItemIdx     = newIdx;
        _hoveredContent     = newContent;
        _hoveringRightPanel = onRight;

        // During drag: update sticky hover-item when the cursor enters a real item.
        // When the cursor moves to a placeholder / header / empty space the previous
        // item keeps displaying ("sticky") so the user can read and reach container
        // content rows without it disappearing.
        if (_isDragging && _member != null && newAnchor.HasValue && newIdx >= 0)
        {
            var items = _member.EquippedItems[newAnchor.Value];
            if (newIdx < items.Count)
            {
                var candidate = items[newIdx];
                if (candidate != _dragHoverItem)
                {
                    _dragHoverItem      = candidate;
                    _dragHoverAnchor    = newAnchor.Value;
                    _dragHoverStartTick = Environment.TickCount64;
                    _dragHoverReady     = false;
                    changed = true;
                }
            }
        }

        return changed;
    }

    /// <summary>
    /// Call every frame.  Returns true when a re-render is needed
    /// (blink toggle or delay-elapsed).
    /// </summary>
    public bool Update()
    {
        // Promote pending drag after the short delay.
        if (_pendingDrag && !_isDragging)
        {
            if (Environment.TickCount64 - _pendingClickTick >= DragStartDelayMs)
            {
                PromotePendingDrag();
                return true;
            }
        }

        if (!_isDragging)
            return false;

        bool needsRender = false;
        long now = Environment.TickCount64;

        // Check if the 1.5 s dwell just elapsed.
        if (_dragHoverItem != null && !_dragHoverReady && now - _dragHoverStartTick >= DragHoverDelayMs)
        {
            _dragHoverReady = true;
            needsRender = true;
        }

        // Toggle blink: during delay (body-art item pulse) and after delay
        // (right-panel container preview pulse).
        if (now - _blinkLastToggle >= BlinkIntervalMs)
        {
            _blinkOn = !_blinkOn;
            _blinkLastToggle = now;
            needsRender = true;
        }

        return needsRender;
    }

    /// <summary>
    /// Process a left mouse-down click.  Starts a drag when an item box is hit.
    /// Returns true when state changed (triggers re-render).
    /// </summary>
    public bool ProcessClick(int x, int y)
    {
        // Clear any deferred header selection from a previous mouse-down.
        _pendingDragAnchor = null;

        int contentIdx = HitTestContent(y);
        if (contentIdx >= 0)
        {
            if (_isDragging) { CancelDrag(); return true; }

            // Mouse-down on a content item: record a *pending* drag.
            // Resolve the currently-viewed container by walking the selection path.
            var viewedContainer = ResolveViewedContainer();
            if (viewedContainer != null && contentIdx < viewedContainer.Contents.Count)
            {
                _pendingDrag          = true;
                _pendingDragItem      = viewedContainer.Contents[contentIdx];
                _pendingDragAnchor    = _selectedAnchor;
                _pendingDragItemIdx   = contentIdx;
                _pendingDragContainer = viewedContainer;
                _pendingClickX        = x;
                _pendingClickY        = y;
                _pendingClickTick     = Environment.TickCount64;
                return true;
            }
            return false;
        }

        var hit = HitTestItem(x, y);
        if (hit.HasValue)
        {
            var (anchor, itemIdx) = (hit.Value.Anchor, hit.Value.ItemIdx);

            if (_isDragging)
            {
                if (anchor != _dragSourceAnchor) CompleteOrCancelDrag(anchor);
                else CancelDrag();
                return true;
            }

            if (itemIdx >= 0 && _member != null)
            {
                var items = _member.EquippedItems[anchor];
                if (itemIdx < items.Count)
                {
                    _pendingDrag          = true;
                    _pendingDragItem      = items[itemIdx];
                    _pendingDragAnchor    = anchor;
                    _pendingDragItemIdx   = itemIdx;
                    _pendingDragContainer = null;
                    _pendingClickX        = x;
                    _pendingClickY        = y;
                    _pendingClickTick     = Environment.TickCount64;
                    // Selection deferred to mouse-up so the right panel
                    // stays on the current container during drag.
                    return true;
                }
            }

            // Header / placeholder — also defer to mouse-up.
            _pendingDrag        = false;  // no item to drag
            _pendingDragAnchor  = anchor;
            _pendingDragItemIdx = -1;     // marks header-only selection
            return true;
        }

        if (_isDragging) { CancelDrag(); return true; }

        _selectedAnchor  = null;
        _selectedItemIdx = -1;
        _selectedContentPath.Clear();
        return false;
    }

    /// <summary>
    /// Process mouse-up.  Always ends the drag:
    ///   • Over a container item → drop into container (immediate, no delay needed).
    ///   • Over a different anchor → drop into anchor.
    ///   • Anywhere else → cancel (item returns).
    /// </summary>
    public bool ProcessMouseUp(int x, int y)
    {
        // Pending drag that never promoted → treat as click-to-select.
        if (_pendingDrag && !_isDragging)
        {
            int idx = _pendingDragItemIdx;
            var anchor = _pendingDragAnchor;
            bool fromContent = _pendingDragContainer != null;
            ClearPendingDrag();
            if (fromContent)
            {
                SelectContent(idx);
            }
            else if (anchor.HasValue)
            {
                _selectedAnchor  = anchor.Value;
                _selectedItemIdx = idx;
                _selectedContentPath.Clear();
            }
            return true;
        }

        // Deferred header/placeholder selection (no pending drag, just anchor click).
        if (!_isDragging && _pendingDragAnchor.HasValue && _pendingDragItemIdx == -1)
        {
            _selectedAnchor  = _pendingDragAnchor.Value;
            _selectedItemIdx = -1;
            _selectedContentPath.Clear();
            _pendingDragAnchor = null;
            return true;
        }

        // When not dragging, mouse-up on a content row selects / drills in.
        if (!_isDragging)
        {
            // Click on the "← item" row → go back one level.
            if (_selectedContentPath.Count > 0 && x >= RightContentX && y == InfoStartRow)
            {
                GoBackContent();
                return true;
            }
            int contentIdx = HitTestContent(y);
            if (contentIdx >= 0)
            {
                SelectContent(contentIdx);
                return true;
            }
            return false;
        }

        if (_dragItem == null || _member == null || !_dragSourceAnchor.HasValue)
        {
            CancelDrag();
            return true;
        }

        // ── Try dropping into a container item. ──
        // Check three sources in priority order:
        //   1. Right-panel container: when cursor is over the right panel, the
        //      container being *viewed* there wins (e.g. pouch inside backpack).
        //   2. Fresh hit on the body-art item boxes.
        //   3. Sticky _dragHoverItem (from a previous hover on body art) — only
        //      when cursor is NOT on the right panel or a body-art item.
        // Skip containers that are the drag source.
        ContainerItem? target = null;
        bool onRightPanel = x >= RightPanelX;

        // 1. Right-panel container (highest priority when cursor is there).
        if (onRightPanel)
        {
            var viewed = ResolveViewedContainer();
            if (viewed != null && viewed != _dragSourceContainer)
                target = viewed;
        }

        // 2. Fresh hit on the body-art item boxes.
        var hit = HitTestItem(x, y);
        if (target == null && hit.HasValue && hit.Value.ItemIdx >= 0)
        {
            var items = _member.EquippedItems[hit.Value.Anchor];
            if (hit.Value.ItemIdx < items.Count && items[hit.Value.ItemIdx] is ContainerItem c
                && c != _dragSourceContainer)
                target = c;
        }

        // 3. Sticky drag-hover item — only when cursor is NOT on a body-art item
        //    or the right panel (so direct targets take priority over stale hover).
        if (target == null && !hit.HasValue && !onRightPanel
            && _dragHoverItem is ContainerItem sticky
            && sticky != _dragSourceContainer)
            target = sticky;

        if (target != null && target != _dragItem && target.TryAdd(_dragItem))
        {
            RemoveDragItemFromSource();
            // Update selection to show the target container — but only when
            // the drop came from the body art, not the right panel (which
            // should keep its current view).
            if (!onRightPanel)
            {
                if (hit.HasValue && hit.Value.ItemIdx >= 0)
                {
                    _selectedAnchor  = hit.Value.Anchor;
                    _selectedItemIdx = hit.Value.ItemIdx;
                }
                else if (_dragHoverAnchor.HasValue)
                {
                    _selectedAnchor  = _dragHoverAnchor;
                    var anchorItems = _member.EquippedItems[_dragHoverAnchor.Value];
                    _selectedItemIdx = anchorItems.IndexOf(target);
                }
                _selectedContentPath.Clear();
            }
            ResetDragState();
            return true;
        }

        // ── Anchor-level drop (different anchor). ──
        if (_hoveredAnchor.HasValue && _hoveredAnchor.Value != _dragSourceAnchor)
            CompleteOrCancelDrag(_hoveredAnchor.Value);
        else
            CancelDrag();

        return true;
    }

    /// <summary>Clear all hover / selection state (called when switching away from this tab).</summary>
    public void ClearHover()
    {
        _hoveredAnchor      = null;
        _hoveredItemIdx     = -1;
        _hoveringRightPanel = false;
        CancelDrag();
    }

    // ═══════════════════════════════════════════════════════════════
    // Drag helpers
    // ═══════════════════════════════════════════════════════════════

    private void CompleteOrCancelDrag(EquipmentAnchor target)
    {
        if (_isDragging && _dragItem != null && _member != null && _dragSourceAnchor.HasValue)
        {
            int avail = _member.AvailableSlots(target);
            if (avail >= _dragItem.SlotCount && target.CanAccept(_dragItem))
            {
                RemoveDragItemFromSource();
                _member.EquippedItems[target].Add(_dragItem);
                // Select the target anchor so the right panel shows it after the drop.
                _selectedAnchor  = target;
                _selectedItemIdx = _member.EquippedItems[target].Count - 1;
                _selectedContentPath.Clear();
            }
        }
        ResetDragState();
    }

    /// <summary>Remove the dragged item from its source (anchor list or container contents).</summary>
    private void RemoveDragItemFromSource()
    {
        if (_dragItem == null || _member == null || !_dragSourceAnchor.HasValue) return;

        if (_dragSourceContainer != null)
            _dragSourceContainer.TryRemove(_dragItem);
        else
            _member.EquippedItems[_dragSourceAnchor.Value].RemoveAt(_dragSourceItemIdx);
    }

    private void CancelDrag()
    {
        ClearPendingDrag();
        ResetDragState();
    }

    private void ClearPendingDrag()
    {
        _pendingDrag          = false;
        _pendingDragItem      = null;
        _pendingDragAnchor    = null;
        _pendingDragItemIdx   = -1;
        _pendingDragContainer = null;
    }

    /// <summary>Promote a pending drag into a real drag.</summary>
    private void PromotePendingDrag()
    {
        if (!_pendingDrag || _pendingDragItem == null) return;
        var item      = _pendingDragItem;
        var anchor    = _pendingDragAnchor;
        int idx       = _pendingDragItemIdx;
        var container = _pendingDragContainer;
        ClearPendingDrag();

        _dragItem             = item;
        _dragSourceAnchor     = anchor;
        _dragSourceItemIdx    = idx;
        _dragSourceContainer  = container;
        _isDragging           = true;
        UpdateDragPopup();
    }

    /// <summary>Drill into a content item at the current depth.</summary>
    private void SelectContent(int contentIdx)
    {
        var viewedContainer = ResolveViewedContainer();
        if (viewedContainer != null && contentIdx < viewedContainer.Contents.Count)
            _selectedContentPath.Add(contentIdx);
    }

    /// <summary>Go back one level in the content selection path.</summary>
    private void GoBackContent()
    {
        if (_selectedContentPath.Count > 0)
            _selectedContentPath.RemoveAt(_selectedContentPath.Count - 1);
    }

    /// <summary>
    /// Walk the selection path to find the container currently being viewed
    /// in the right panel. Returns null when no container is in view.
    /// </summary>
    private ContainerItem? ResolveViewedContainer()
    {
        if (_member == null || !_selectedAnchor.HasValue || _selectedItemIdx < 0) return null;
        var items = _member.EquippedItems[_selectedAnchor.Value];
        if (_selectedItemIdx >= items.Count) return null;
        if (items[_selectedItemIdx] is not ContainerItem current) return null;

        foreach (int idx in _selectedContentPath)
        {
            if (idx >= current.Contents.Count) return null;
            if (current.Contents[idx] is ContainerItem nested)
                current = nested;
            else
                return current;  // non-container selected; parent is the viewed container
        }
        return current;
    }

    private void ResetDragState()
    {
        _isDragging          = false;
        _dragItem            = null;
        _dragSourceAnchor    = null;
        _dragSourceItemIdx   = -1;
        _dragSourceContainer = null;
        _dragHoverItem       = null;
        _dragHoverAnchor    = null;
        _dragHoverStartTick = 0;
        _dragHoverReady     = false;
        _blinkOn            = false;
        _blinkLastToggle    = 0;
        _popup?.Clear();
    }

    // Popup dimensions for the floating drag-feedback box
    private const int DragPopupW = 14;

    /// <summary>Draw the dragged item name in the popup so it follows the cursor.</summary>
    private void UpdateDragPopup()
    {
        if (_popup == null || _dragItem == null) return;
        int boxH = _dragItem.SlotCount;
        _popup.Clear();
        _popup.Fill(0, 0, DragPopupW, boxH, ' ', DragTextColor, DragBoxBg);
        _popup.DrawBox(0, 0, DragPopupW, boxH, DragBoxBorder, DragBoxBg);
        string name = TruncRight(_dragItem.DisplayName, DragPopupW - 2);
        int textX = 1 + (DragPopupW - 2 - name.Length) / 2;
        _popup.DrawText(textX, boxH / 2, name, DragTextColor, DragBoxBg);
    }

    // ═══════════════════════════════════════════════════════════════
    // Anchor rendering
    // ═══════════════════════════════════════════════════════════════

    private void RenderAnchor(PartyMember member, EquipmentAnchor anchor)
    {
        if (!_gearData.TryGetPosition(anchor, out int artRow, out int artCol))
            return;

        int termX   = ComputeBoxX(artCol);
        int headerY = artRow;
        if (headerY < 0 || headerY > 98) return;

        // Highlight as drop target only when dragging over a *different* anchor.
        bool isDropTarget = _isDragging && _hoveredAnchor == anchor && anchor != _dragSourceAnchor;

        // Gray out anchors that cannot accept the dragged item.
        bool isGrayedOut = _isDragging && _dragItem != null && !anchor.CanAccept(_dragItem)
                           && anchor != _dragSourceAnchor;

        // ── Header row ────────────────────────────────────────────
        Vector4 hdrBg = isGrayedOut ? GrayHeaderBg : isDropTarget ? DropTargetHeaderBg : HeaderBg;
        Vector4 hdrFg = isGrayedOut ? GrayHeaderFg : isDropTarget ? DropTargetHeaderFg : HeaderFg;

        for (int dx = 0; dx < ItemBoxW; dx++)
            _terminal.SetCell(termX + dx, headerY, ' ', hdrFg, hdrBg);

        string headerText = anchor.Label().ToUpperInvariant();
        if (headerText.Length > ItemBoxW - 2)
            headerText = TruncRight(headerText, ItemBoxW - 2);
        int labelX = termX + 1 + (ItemBoxW - 2 - headerText.Length) / 2;
        _terminal.Text(labelX, headerY, headerText, hdrFg, hdrBg);

        // Header row is also a valid drop target / hover region.
        _itemHits.Add(new ItemHit(anchor, -1, termX, headerY, termX + ItemBoxW - 1, headerY));

        // ── Item boxes ────────────────────────────────────────────
        var items = member.EquippedItems[anchor];
        int itemY = headerY + 1;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            int boxH = item.SlotCount;
            if (itemY + boxH > 100) break;

            bool isBeingDragged = _isDragging && _dragSourceContainer == null
                                  && _dragSourceAnchor == anchor && _dragSourceItemIdx == i;
            bool isHovered      = !_isDragging && _hoveredAnchor == anchor && _hoveredItemIdx == i;
            bool isSelected     = _selectedAnchor == anchor && _selectedItemIdx == i;
            // Blink: this item is the drag-hover target but the delay hasn't elapsed yet.
            bool isBlinking     = _isDragging && !_dragHoverReady && _dragHoverItem == item
                                  && _dragHoverAnchor == anchor;

            Vector4 border, bg, textCol;
            if (isBeingDragged)
            {
                border = DragBoxBorder; bg = DragBoxBg; textCol = DragTextColor;
            }
            else if (isGrayedOut
                     && !(item is ContainerItem c && _dragItem != null && c.CanContain(_dragItem)
                          && c.AvailableSlots >= _dragItem.SlotCount && c != _dragItem))
            {
                border = GrayBoxBorder; bg = GrayBoxBg; textCol = GrayTextColor;
            }
            else if (isBlinking)
            {
                border = _blinkOn ? BlinkOnBorder  : BlinkOffBorder;
                bg     = _blinkOn ? BlinkOnBg      : BlinkOffBg;
                textCol = _blinkOn ? BlinkOnText    : BlinkOffText;
            }
            else if (_dragHoverReady && _dragHoverItem == item && _dragHoverAnchor == anchor)
            {
                // Solid highlight once the dwell completes.
                border = HoveredBoxBorder; bg = HoveredBoxBg; textCol = HoveredTextColor;
            }
            else if (isDropTarget)
            {
                border = DropTargetBorder; bg = DropTargetBg; textCol = HoveredTextColor;
            }
            else if (isSelected)
            {
                border = SelectedBoxBorder; bg = SelectedBoxBg; textCol = SelectedTextColor;
            }
            else if (isHovered)
            {
                border = HoveredBoxBorder; bg = HoveredBoxBg; textCol = HoveredTextColor;
            }
            else
            {
                border = NormalBoxBorder; bg = NormalBoxBg; textCol = NormalTextColor;
            }

            DrawItemBox(item.DisplayName, termX, itemY, boxH, border, bg, textCol);
            _itemHits.Add(new ItemHit(anchor, i, termX, itemY, termX + ItemBoxW - 1, itemY + boxH - 1));
            itemY += boxH;
        }

        // ── Preview box for dragged item (blinking, shows item as if placed) ──
        int remaining  = member.AvailableSlots(anchor);
        bool showPreview = isDropTarget && !isGrayedOut && _dragItem != null
                           && _dragItem.SlotCount <= remaining;

        if (showPreview)
        {
            var pvBorder = _blinkOn ? Config.Colors.BrightYellow : Config.Colors.LightGray75;
            var pvBg     = _blinkOn ? new Vector4(0.14f, 0.12f, 0.02f, 1f) : new Vector4(0.08f, 0.07f, 0.02f, 1f);
            var pvFg     = _blinkOn ? Config.Colors.BrightYellow : Config.Colors.White;
            int pvH = _dragItem!.SlotCount;
            if (itemY + pvH <= 100)
            {
                DrawItemBox(_dragItem.DisplayName, termX, itemY, pvH, pvBorder, pvBg, pvFg);
                _itemHits.Add(new ItemHit(anchor, -1, termX, itemY, termX + ItemBoxW - 1, itemY + pvH - 1));
                itemY += pvH;
            }
            remaining -= _dragItem.SlotCount;
        }

        // ── Placeholder boxes (remaining capacity, one per 3-slot chunk) ──
        int phCount    = remaining / PlaceholderH;

        for (int p = 0; p < phCount; p++)
        {
            if (itemY + PlaceholderH > 100) break;

            Vector4 phBorder = isGrayedOut ? GrayBoxBorder : EmptyBoxBorder;
            Vector4 phBg     = isGrayedOut ? GrayBoxBg     : EmptyBoxBg;
            Vector4 phText   = isGrayedOut ? GrayTextColor : EmptyTextColor;

            DrawItemBox(string.Empty, termX, itemY, PlaceholderH, phBorder, phBg, phText);
            _itemHits.Add(new ItemHit(anchor, -1, termX, itemY, termX + ItemBoxW - 1, itemY + PlaceholderH - 1));
            itemY += PlaceholderH;
        }
    }

    private void DrawItemBox(string label, int x, int y, int h, Vector4 border, Vector4 bg, Vector4 fg)
    {
        for (int dy = 0; dy < h; dy++)
            for (int dx = 0; dx < ItemBoxW; dx++)
                _terminal.SetCell(x + dx, y + dy, ' ', fg, bg);

        _terminal.DrawBox(x, y, ItemBoxW, h, BoxStyle.Single, border, bg);

        if (!string.IsNullOrEmpty(label))
        {
            int innerW = ItemBoxW - 2;
            if (label.Length > innerW) label = TruncRight(label, innerW);
            int lx = x + 1 + (innerW - label.Length) / 2;
            _terminal.Text(lx, y + h / 2, label, fg, bg);
        }
    }

    private static int ComputeBoxX(int artCol)
    {
        int ideal = ArtOffsetX + artCol - ItemBoxW / 2;
        return Math.Clamp(ideal, ArtOffsetX, ArtOffsetX + ArtWidth - ItemBoxW);
    }

    // ═══════════════════════════════════════════════════════════════
    // Right info panel
    // ═══════════════════════════════════════════════════════════════

    private void RenderRightPanel(PartyMember member)
    {
        if (!_selectedAnchor.HasValue)
        {
            DrawInfoHint();
            return;
        }

        var anchor = _selectedAnchor.Value;
        var items  = member.EquippedItems[anchor];

        // Compact drag banner at the top when dragging (doesn't replace the panel).
        int panelStartRow = InfoStartRow;
        if (_isDragging && _dragItem != null)
        {
            panelStartRow = DrawDragBanner(panelStartRow);
        }

        if (_selectedItemIdx >= 0 && _selectedItemIdx < items.Count)
        {
            var selectedItem = items[_selectedItemIdx];

            // Walk the content selection path to find what to display.
            Item displayItem = selectedItem;
            bool hasPath = false;

            if (selectedItem is ContainerItem root)
            {
                ContainerItem current = root;
                for (int d = 0; d < _selectedContentPath.Count; d++)
                {
                    int ci = _selectedContentPath[d];
                    if (ci >= current.Contents.Count) break;
                    displayItem = current.Contents[ci];
                    hasPath = true;
                    if (displayItem is ContainerItem next)
                        current = next;
                    else
                        break;
                }

                // Show the deepest selected item, or the root if no path.
                int nextRow = DrawItemInfo(displayItem, panelStartRow, showBack: hasPath);
                // Show drag preview only when the mouse is over the right panel.
                if (displayItem is ContainerItem viewContainer)
                {
                    if (_isDragging && _hoveringRightPanel)
                        DrawContainerContentsDragPreview(viewContainer, nextRow);
                    else
                        DrawContainerContents(viewContainer, nextRow);
                }
            }
            else
            {
                DrawItemInfo(selectedItem, panelStartRow, showBack: false);
            }
        }
        else
        {
            DrawAnchorInfo(anchor, items, member);
        }
    }

    /// <summary>Draw a compact drag banner at the top of the right panel. Returns the next available row.</summary>
    private int DrawDragBanner(int startRow)
    {
        int y = startRow;
        var dragFg = new Vector4(0.70f, 0.60f, 0.15f, 1f);
        string name = _dragItem != null ? TruncRight(_dragItem.DisplayName, RightContentW - 12) : "?";
        _terminal.Text(RightContentX, y, $"Dragging: {name}", dragFg, RightPanelBg);
        y++;
        DrawSep(y); y++;
        return y;
    }

    private void DrawInfoHint()
    {
        int y = InfoStartRow + 6;
        _terminal.Text(RightContentX, y,     "Select an anchor slot",  InfoLabelColor, RightPanelBg);
        _terminal.Text(RightContentX, y + 1, "to inspect its items.",  InfoLabelColor, RightPanelBg);
    }

    private void DrawAnchorInfo(EquipmentAnchor anchor, List<Item> items, PartyMember member)
    {
        int y = InfoStartRow;
        _terminal.Text(RightContentX, y, anchor.Label().ToUpperInvariant(), InfoTitleColor, RightPanelBg); y++;

        int used = member.UsedSlots(anchor);
        int cap  = anchor.Capacity();
        DrawKV("Slots", $"{used}/{cap} used", ref y);
        y++;

        if (items.Count == 0)
        {
            _terminal.Text(RightContentX, y, "(empty)", EmptyTextColor, RightPanelBg);
            return;
        }

        DrawSep(y); y++;
        _terminal.Text(RightContentX, y, "Equipped:", InfoLabelColor, RightPanelBg); y++;
        foreach (var item in items)
        {
            if (y > InfoEndRow) break;
            _terminal.Text(RightContentX, y++,
                TruncRight($"\u2022 {item.DisplayName}  ({item.SlotCount} sl)", RightContentW),
                InfoValueColor, RightPanelBg);
        }
    }

    private int DrawItemInfo(Item item, int startRow, bool showBack)
    {
        int y = startRow;

        if (showBack)
        {
            _terminal.Text(RightContentX, y, "\u2190 item", InfoLabelColor, RightPanelBg);
            y++;
        }

        _terminal.Text(RightContentX, y, TruncRight(item.DisplayName, RightContentW), InfoTitleColor, RightPanelBg);
        y++;
        DrawSep(y); y++;

        string types = string.Join(", ", item.Types.Select(t => t.ToString()));
        DrawKV("Type",   types,                                       ref y);
        DrawKV("Weight", $"{item.Weight:F1} kg",                      ref y);
        DrawKV("Size",   $"{item.Size}  ({item.SlotCount} slots)",    ref y);
        y++;

        foreach (string desc in WrapText(item.Description, RightContentW))
        {
            if (y > InfoEndRow) break;
            _terminal.Text(RightContentX, y++, desc, InfoValueColor, RightPanelBg);
        }
        y++;

        if (item.Info.Length > 0)
        {
            DrawSep(y); y++;
            foreach (string line in item.Info)
            {
                if (y > InfoEndRow) break;
                foreach (string wrapped in WrapText(line, RightContentW))
                {
                    if (y > InfoEndRow) break;
                    _terminal.Text(RightContentX, y++, wrapped, InfoLabelColor, RightPanelBg);
                }
            }
            y++;
        }

        return y;
    }

    // Box width for items rendered inside the right info panel.
    private const int ContentBoxW = RightContentW;

    private void DrawContainerContents(ContainerItem container, int startRow)
    {
        _contentHits.Clear();

        int y = startRow;
        if (y > InfoEndRow) return;

        DrawSep(y); y++;
        string header = $"Contents  {container.UsedSlots}/{container.ContentSlots} slots";
        _terminal.Text(RightContentX, y, TruncRight(header, RightContentW), InfoLabelColor, RightPanelBg);
        y++;

        if (container.Contents.Count == 0)
        {
            _terminal.Text(RightContentX, y, "(empty)", EmptyTextColor, RightPanelBg);
            y++;
        }
        else
        {
            for (int i = 0; i < container.Contents.Count; i++)
            {
                var ci = container.Contents[i];
                int boxH = ci.SlotCount;
                if (y + boxH > InfoEndRow) break;

                bool isHov = _hoveredContent  == i;

                Vector4 border = isHov ? HoveredBoxBorder : NormalBoxBorder;
                Vector4 bg     = isHov ? HoveredBoxBg     : NormalBoxBg;
                Vector4 fg     = isHov ? HoveredTextColor  : NormalTextColor;

                DrawContentItemBox(ci.DisplayName, RightContentX, y, boxH, border, bg, fg);
                _contentHits.Add(new ContentHit(i, y, y + boxH - 1));
                y += boxH;
            }
        }

        // Placeholder boxes for each 3-slot chunk of remaining capacity
        int remaining = container.ContentSlots - container.UsedSlots;
        int phCount   = remaining / PlaceholderH;
        for (int p = 0; p < phCount && y + PlaceholderH <= InfoEndRow; p++)
        {
            DrawContentItemBox(string.Empty, RightContentX, y, PlaceholderH, EmptyBoxBorder, EmptyBoxBg, EmptyTextColor);
            y += PlaceholderH;
        }
    }

    /// <summary>
    /// Draws container contents with a preview box showing where the dragged item
    /// would land if the user releases the mouse button.
    /// </summary>
    private void DrawContainerContentsDragPreview(ContainerItem container, int startRow)
    {
        int y = startRow;
        if (y > InfoEndRow) return;

        bool canDrop = _dragItem != null && container.CanContain(_dragItem)
                       && _dragItem.SlotCount <= container.AvailableSlots
                       && container != _dragItem;

        int usedPreview = container.UsedSlots + (_dragItem != null && canDrop ? _dragItem.SlotCount : 0);
        DrawSep(y); y++;
        string header = canDrop
            ? $"Contents  {usedPreview}/{container.ContentSlots} slots (preview)"
            : $"Contents  {container.UsedSlots}/{container.ContentSlots} slots";
        _terminal.Text(RightContentX, y, TruncRight(header, RightContentW), InfoLabelColor, RightPanelBg);
        y++;

        // Existing items as boxes
        _contentHits.Clear();
        for (int i = 0; i < container.Contents.Count; i++)
        {
            var ci = container.Contents[i];
            int boxH = ci.SlotCount;
            if (y + boxH > InfoEndRow) break;
            DrawContentItemBox(ci.DisplayName, RightContentX, y, boxH, NormalBoxBorder, NormalBoxBg, NormalTextColor);
            _contentHits.Add(new ContentHit(i, y, y + boxH - 1));
            y += boxH;
        }

        // Preview box for the dragged item (blinking yellow / white)
        if (canDrop && _dragItem != null)
        {
            var previewBorder = _blinkOn ? Config.Colors.BrightYellow : Config.Colors.LightGray75;
            var previewBg     = _blinkOn ? new Vector4(0.14f, 0.12f, 0.02f, 1f) : new Vector4(0.08f, 0.07f, 0.02f, 1f);
            var previewFg     = _blinkOn ? Config.Colors.BrightYellow : Config.Colors.White;
            int boxH = _dragItem.SlotCount;
            if (y + boxH <= InfoEndRow)
            {
                DrawContentItemBox(_dragItem.DisplayName, RightContentX, y, boxH, previewBorder, previewBg, previewFg);
                y += boxH;
            }
        }
        else if (!canDrop && _dragItem != null && y <= InfoEndRow)
        {
            var noFitFg = new Vector4(0.90f, 0.30f, 0.30f, 1f);
            string reason = !container.CanContain(_dragItem) ? "\u2717 Type not accepted"
                          : _dragItem.SlotCount > container.AvailableSlots ? "\u2717 No room"
                          : "\u2717 Cannot drop";
            _terminal.Text(RightContentX, y++, reason, noFitFg, RightPanelBg);
        }

        // Remaining placeholders
        int remaining = container.ContentSlots - container.UsedSlots;
        if (canDrop && _dragItem != null) remaining -= _dragItem.SlotCount;
        int phCount = remaining / PlaceholderH;
        for (int p = 0; p < phCount && y + PlaceholderH <= InfoEndRow; p++)
        {
            DrawContentItemBox(string.Empty, RightContentX, y, PlaceholderH, EmptyBoxBorder, EmptyBoxBg, EmptyTextColor);
            y += PlaceholderH;
        }
    }

    /// <summary>Draw an item box in the right panel (full content width).</summary>
    private void DrawContentItemBox(string label, int x, int y, int h, Vector4 border, Vector4 bg, Vector4 fg)
    {
        for (int dy = 0; dy < h; dy++)
            for (int dx = 0; dx < ContentBoxW; dx++)
                _terminal.SetCell(x + dx, y + dy, ' ', fg, bg);

        _terminal.DrawBox(x, y, ContentBoxW, h, BoxStyle.Single, border, bg);

        if (!string.IsNullOrEmpty(label))
        {
            int innerW = ContentBoxW - 2;
            if (label.Length > innerW) label = TruncRight(label, innerW);
            int lx = x + 1 + (innerW - label.Length) / 2;
            _terminal.Text(lx, y + h / 2, label, fg, bg);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Hit testing
    // ═══════════════════════════════════════════════════════════════

    private ItemHit? HitTestItem(int x, int y)
    {
        foreach (var hit in _itemHits)
            if (x >= hit.X0 && x <= hit.X1 && y >= hit.Y0 && y <= hit.Y1)
                return hit;
        return null;
    }

    private int HitTestContent(int y)
    {
        foreach (var hit in _contentHits)
            if (y >= hit.Y0 && y <= hit.Y1)
                return hit.Index;
        return -1;
    }

    // ═══════════════════════════════════════════════════════════════
    // Drawing helpers
    // ═══════════════════════════════════════════════════════════════

    private void DrawSep(int y)
    {
        _terminal.Text(RightContentX, y, new string('\u2500', RightContentW), SepColor, RightPanelBg);
    }

    private void DrawKV(string label, string value, ref int y)
    {
        if (y > InfoEndRow) return;
        string labelPad = (label + ":").PadRight(9);
        _terminal.Text(RightContentX,     y, labelPad,                              InfoLabelColor, RightPanelBg);
        _terminal.Text(RightContentX + 9, y, TruncRight(value, RightContentW - 9),  InfoValueColor, RightPanelBg);
        y++;
    }

    private static string TruncRight(string s, int maxLen)
    {
        if (maxLen <= 0) return string.Empty;
        if (s.Length <= maxLen) return s;
        return s[..(maxLen - 1)] + "\u2026";
    }

    private static IEnumerable<string> WrapText(string text, int width)
    {
        if (string.IsNullOrEmpty(text)) yield break;
        int pos = 0;
        while (pos < text.Length)
        {
            int take = Math.Min(width, text.Length - pos);
            if (pos + take < text.Length)
            {
                int lastSpace = text.LastIndexOf(' ', pos + take, take);
                if (lastSpace > pos) take = lastSpace - pos;
            }
            yield return text.Substring(pos, take).TrimEnd();
            pos += take;
            if (pos < text.Length && text[pos] == ' ') pos++;
        }
    }
}
