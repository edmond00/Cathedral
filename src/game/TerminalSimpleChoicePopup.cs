using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Terminal;

namespace Cathedral.Game;

/// <summary>
/// Terminal-based popup for a short list of plain-text choices (e.g. "Think / Observe").
/// Fixed at click position, shows options with hover highlighting.
/// </summary>
public class TerminalSimpleChoicePopup
{
    private const int POPUP_WIDTH = 28;

    private readonly PopupTerminalHUD _popup;
    private List<string> _choices = new();
    private HashSet<int> _disabledIndices = new();
    private int? _hoveredIndex = null;
    private Vector2 _fixedPosition;
    private string _title = "";

    private static readonly Vector4 BorderColor = Config.Colors.MediumGray60;
    private static readonly Vector4 TitleColor = Config.Colors.DarkYellowGrey;
    private static readonly Vector4 DisabledColor = Config.Colors.DarkGray35;

    public TerminalSimpleChoicePopup(PopupTerminalHUD popup)
    {
        _popup = popup ?? throw new ArgumentNullException(nameof(popup));
    }

    /// <summary>
    /// Show the popup at a screen position with a list of choice labels.
    /// <paramref name="disabledIndices"/> entries are rendered greyed-out and cannot be clicked.
    /// </summary>
    public void Show(Vector2 screenPosition, List<string> choices, string title, HashSet<int>? disabledIndices = null)
    {
        _choices = choices ?? throw new ArgumentNullException(nameof(choices));
        _disabledIndices = disabledIndices ?? new HashSet<int>();
        _fixedPosition = screenPosition;
        _hoveredIndex = null;
        _title = title;

        _popup.SetMousePosition(_fixedPosition);
        _popup.SetFixedMode(true);

        Render();
    }

    /// <summary>Hide and clear the popup.</summary>
    public void Hide()
    {
        _popup.Clear();
        _popup.SetFixedMode(false);
        _choices.Clear();
        _disabledIndices.Clear();
        _hoveredIndex = null;
    }

    /// <summary>True while the popup is displayed.</summary>
    public bool IsVisible => _choices.Count > 0;

    /// <summary>Update hover highlight from screen pixel coordinates. Returns true if repaint needed.</summary>
    public bool UpdateHover(float screenX, float screenY, Vector2i windowSize, float cellPixelSize)
    {
        if (!IsVisible) return false;

        int? newHoveredIndex = GetIndexAtPosition(screenX, screenY, windowSize, cellPixelSize);
        if (newHoveredIndex != _hoveredIndex)
        {
            _hoveredIndex = newHoveredIndex;
            Render();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Handle a click at screen pixel coordinates.
    /// Returns the 0-based choice index if a choice was clicked, or null if clicked outside.
    /// The popup is hidden either way.
    /// </summary>
    public int? HandleClick(float screenX, float screenY, Vector2i windowSize, float cellPixelSize)
    {
        if (!IsVisible) return null;

        int? index = GetIndexAtPosition(screenX, screenY, windowSize, cellPixelSize);
        Hide();
        return index;
    }

    // ── Internals ────────────────────────────────────────────────

    private int? GetIndexAtPosition(float screenX, float screenY, Vector2i windowSize, float cellPixelSize)
    {
        var bounds = _popup.GetScreenBounds(windowSize);
        if (bounds == null) return null;

        var (left, top, right, bottom) = bounds.Value;
        if (screenX < left || screenX > right || screenY < top || screenY > bottom)
            return null;

        float relativeY = screenY - top;
        int cellY = (int)Math.Floor((relativeY + cellPixelSize * 0.5f) / cellPixelSize);

        // Row 0 = title border, rows 1..Count = choices
        if (cellY < 1 || cellY > _choices.Count)
            return null;

        int index = cellY - 1;
        if (index >= _choices.Count || _disabledIndices.Contains(index))
            return null;

        return index;
    }

    private void Render()
    {
        _popup.Clear();
        if (!IsVisible) return;

        int popupHeight = _choices.Count + 3; // border-title + choices + close-hint + border

        _popup.Fill(0, 0, POPUP_WIDTH, popupHeight, ' ',
            Config.ThinkingModusMentisPopup.ModusMentisNormalColor,
            Config.ThinkingModusMentisPopup.BackgroundColor);

        _popup.DrawBox(0, 0, POPUP_WIDTH, popupHeight,
            BorderColor,
            Config.ThinkingModusMentisPopup.BackgroundColor);

        int titleX = Math.Max(1, (POPUP_WIDTH - _title.Length) / 2);
        _popup.DrawText(titleX, 0, _title, TitleColor, Config.ThinkingModusMentisPopup.BackgroundColor);

        for (int i = 0; i < _choices.Count; i++)
        {
            int displayRow = i + 1;
            bool isDisabled = _disabledIndices.Contains(i);
            bool isHovered = !isDisabled && _hoveredIndex == i;
            Vector4 textColor = isDisabled
                ? DisabledColor
                : isHovered
                    ? Config.ThinkingModusMentisPopup.ModusMentisHoverColor
                    : Config.ThinkingModusMentisPopup.ModusMentisNormalColor;
            Vector4 bgColor = isHovered
                ? Config.ThinkingModusMentisPopup.ModusMentisHoverBackgroundColor
                : Config.ThinkingModusMentisPopup.BackgroundColor;

            string prefix = isHovered ? "> " : "  ";
            string displayText = prefix + _choices[i];

            int maxTextWidth = POPUP_WIDTH - 4;
            if (displayText.Length > maxTextWidth)
                displayText = displayText.Substring(0, maxTextWidth - 3) + "...";

            _popup.Fill(1, displayRow, POPUP_WIDTH - 2, 1, ' ', textColor, bgColor);
            _popup.DrawText(2, displayRow, displayText, textColor, bgColor);
        }

        string closeHint = "[Click to close]";
        int hintX = (POPUP_WIDTH - closeHint.Length) / 2;
        _popup.DrawText(hintX, popupHeight - 1, closeHint,
            Config.NarrativeUI.HintTextColor,
            Config.ThinkingModusMentisPopup.BackgroundColor);
    }
}
