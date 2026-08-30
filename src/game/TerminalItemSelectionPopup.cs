using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// Terminal-based popup for selecting an item to combine with an action.
/// Fixed at click position, shows list of available items with hover highlighting.
/// Mirrors TerminalThinkingModusMentisPopup in structure.
/// </summary>
public class TerminalItemSelectionPopup
{
    private const int POPUP_WIDTH = 38;

    /// <summary>
    /// How many items fit in the box at once — the popup grid's height less the title row, the close
    /// hint and the bottom border. It was a flat 15 with <see cref="_scrollOffset"/> declared and
    /// never written, so a pack holding more than fifteen combinable items simply could not offer
    /// the rest. Same fault, same fix, as TerminalThinkingModusMentisPopup.
    /// </summary>
    private static int MaxVisibleItems => Math.Max(1, Config.Terminal.PopupHeight - 3);

    private readonly PopupTerminalHUD _popup;
    private List<Item> _items = new();
    private int? _hoveredItemIndex = null;
    private Vector2 _fixedPosition;
    private int _scrollOffset = 0;
    private string _title = "Use with this action";

    // Colors reuse the modus mentis popup config
    private static readonly Vector4 BorderColor = Config.Colors.MediumGray60;
    private static readonly Vector4 TitleColor = Config.Colors.DarkYellowGrey;

    public TerminalItemSelectionPopup(PopupTerminalHUD popup)
    {
        _popup = popup ?? throw new ArgumentNullException(nameof(popup));
    }

    /// <summary>
    /// Show the popup at a fixed screen position with the list of items.
    /// </summary>
    public void Show(Vector2 screenPosition, List<Item> items, string title = "Use with this action")
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _fixedPosition = screenPosition;
        _hoveredItemIndex = null;
        _scrollOffset = 0;
        _title = title;

        _popup.SetMousePosition(_fixedPosition);
        _popup.SetFixedMode(true);

        Render();
    }

    /// <summary>Hide the popup.</summary>
    public void Hide()
    {
        _popup.Clear();
        _popup.SetFixedMode(false);
        _items.Clear();
        _hoveredItemIndex = null;
    }

    /// <summary>Whether the popup is currently visible.</summary>
    public bool IsVisible => _items.Count > 0;

    /// <summary>The items currently offered (for --cli listing/selection by index).</summary>
    public IReadOnlyList<Item> Choices => _items;

    /// <summary>How many rows the box shows at the current list length.</summary>
    private int VisibleCount => Math.Min(MaxVisibleItems, _items.Count);

    /// <summary>
    /// Scrolls the list by <paramref name="lines"/> (negative = toward the top), clamped so the last
    /// screenful is the furthest the box can go. Returns true when something moved.
    /// </summary>
    public bool Scroll(int lines)
    {
        if (!IsVisible) return false;

        int maxOffset = Math.Max(0, _items.Count - MaxVisibleItems);
        int next = Math.Clamp(_scrollOffset + lines, 0, maxOffset);
        if (next == _scrollOffset) return false;

        _scrollOffset = next;
        _hoveredItemIndex = null;
        Render();
        return true;
    }

    /// <summary>
    /// Update hover state based on screen pixel mouse position.
    /// Returns true if hover state changed.
    /// </summary>
    public bool UpdateHover(float screenX, float screenY, Vector2i windowSize, float cellPixelSize)
    {
        if (!IsVisible)
            return false;

        int? newHoveredIndex = GetItemIndexAtPosition(screenX, screenY, windowSize, cellPixelSize);

        if (newHoveredIndex != _hoveredItemIndex)
        {
            _hoveredItemIndex = newHoveredIndex;
            Render();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handle click at the given screen pixel position.
    /// Returns the selected item, or null if clicked outside (closes the popup).
    /// </summary>
    public Item? HandleClick(float screenX, float screenY, Vector2i windowSize, float cellPixelSize)
    {
        if (!IsVisible)
            return null;

        int? itemIndex = GetItemIndexAtPosition(screenX, screenY, windowSize, cellPixelSize);

        if (itemIndex.HasValue)
        {
            var selected = _items[itemIndex.Value];
            Hide();
            return selected;
        }
        else
        {
            Hide();
            return null;
        }
    }

    private int? GetItemIndexAtPosition(float screenX, float screenY, Vector2i windowSize, float cellPixelSize)
    {
        var bounds = _popup.GetScreenBounds(windowSize);
        if (bounds == null)
            return null;

        var (left, top, right, bottom) = bounds.Value;

        if (screenX < left || screenX > right || screenY < top || screenY > bottom)
            return null;

        float relativeX = screenX - left;
        float relativeY = screenY - top;
        int cellX = (int)Math.Floor((relativeX + cellPixelSize * 0.5f) / cellPixelSize);
        int cellY = (int)Math.Floor((relativeY + cellPixelSize * 0.5f) / cellPixelSize);

        if (cellY < 1 || cellY > VisibleCount || cellX < 0 || cellX >= POPUP_WIDTH)
            return null;

        int itemIndex = (cellY - 1) + _scrollOffset;

        if (itemIndex >= 0 && itemIndex < _items.Count)
            return itemIndex;

        return null;
    }

    private void Render()
    {
        _popup.Clear();

        if (!IsVisible)
            return;

        int visibleCount = VisibleCount;
        int popupHeight = visibleCount + 3; // title row + items + close hint

        _popup.Fill(0, 0, POPUP_WIDTH, popupHeight, ' ', Config.ThinkingModusMentisPopup.ModusMentisNormalColor, Config.ThinkingModusMentisPopup.BackgroundColor);
        _popup.DrawBox(0, 0, POPUP_WIDTH, popupHeight, BorderColor, Config.ThinkingModusMentisPopup.BackgroundColor);

        // When the list overflows the box the title carries the position, so the player can see
        // that more exist and that the wheel reaches them.
        string title = _items.Count > visibleCount
            ? $"{_title} ({_scrollOffset + 1}-{_scrollOffset + visibleCount}/{_items.Count})"
            : _title;
        if (title.Length > POPUP_WIDTH - 2) title = title.Substring(0, POPUP_WIDTH - 2);
        int titleX = Math.Max(1, (POPUP_WIDTH - title.Length) / 2);
        _popup.DrawText(titleX, 0, title, TitleColor, Config.ThinkingModusMentisPopup.BackgroundColor);

        int startIndex = _scrollOffset;
        int endIndex = Math.Min(startIndex + visibleCount, _items.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            int displayRow = i - startIndex + 1;
            var item = _items[i];

            bool isHovered = _hoveredItemIndex == i;
            Vector4 textColor = isHovered ? Config.ThinkingModusMentisPopup.ModusMentisHoverColor : Config.ThinkingModusMentisPopup.ModusMentisNormalColor;
            Vector4 bgColor = isHovered ? Config.ThinkingModusMentisPopup.ModusMentisHoverBackgroundColor : Config.ThinkingModusMentisPopup.BackgroundColor;

            string prefix = isHovered ? "> " : "  ";
            string label = $"{prefix}{item.DisplayName}";

            int maxTextWidth = POPUP_WIDTH - 4;
            if (label.Length > maxTextWidth)
                label = label.Substring(0, maxTextWidth - 3) + "...";

            _popup.Fill(1, displayRow, POPUP_WIDTH - 2, 1, ' ', textColor, bgColor);
            _popup.DrawText(2, displayRow, label, textColor, bgColor);
        }

        string closeHint = "[ESC or Click to close]";
        int hintX = (POPUP_WIDTH - closeHint.Length) / 2;
        _popup.DrawText(hintX, popupHeight - 1, closeHint,
            Config.NarrativeUI.HintTextColor, Config.ThinkingModusMentisPopup.BackgroundColor);
    }
}
