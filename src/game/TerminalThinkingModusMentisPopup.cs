using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// Terminal-based popup for selecting a thinking modusMentis.
/// Fixed at click position, shows list of modiMentis with hover highlighting.
/// </summary>
public class TerminalThinkingModusMentisPopup
{
    private const int POPUP_WIDTH = 35;
    private const int MAX_VISIBLE_SKILLS = 15;
    
    private readonly PopupTerminalHUD _popup;
    private List<ModusMentis> _thinkingModiMentis = new();
    private int? _hoveredModusMentisIndex = null;
    private Vector2 _fixedPosition;
    private int _scrollOffset = 0;
    private string _title = "Select Thinking ModusMentis";
    
    // Colors from centralized config
    private static readonly Vector4 BorderColor = Config.Colors.MediumGray60;
    private static readonly Vector4 TitleColor = Config.Colors.DarkYellowGrey;
    
    public TerminalThinkingModusMentisPopup(PopupTerminalHUD popup)
    {
        _popup = popup ?? throw new ArgumentNullException(nameof(popup));
    }
    
    /// <summary>
    /// Show the popup at a fixed screen position with the list of modiMentis.
    /// </summary>
    /// <param name="screenPosition">Screen position to show popup at</param>
    /// <param name="modiMentis">List of modiMentis to display</param>
    /// <param name="title">Optional custom title (defaults to "Select Thinking ModusMentis")</param>
    public void Show(Vector2 screenPosition, List<ModusMentis> modiMentis, string title = "Select Thinking ModusMentis")
    {
        _thinkingModiMentis = modiMentis ?? throw new ArgumentNullException(nameof(modiMentis));
        _fixedPosition = screenPosition;
        _hoveredModusMentisIndex = null;
        _scrollOffset = 0;
        _title = title;
        
        // Set popup to fixed position and enable fixed mode
        _popup.SetMousePosition(_fixedPosition);
        _popup.SetFixedMode(true);
        
        Render();
    }
    
    /// <summary>
    /// Hide the popup.
    /// </summary>
    public void Hide()
    {
        _popup.Clear();
        _popup.SetFixedMode(false); // Return to mouse-following mode
        _thinkingModiMentis.Clear();
        _hoveredModusMentisIndex = null;
    }
    
    /// <summary>
    /// Check if the popup is currently visible.
    /// </summary>
    public bool IsVisible => _thinkingModiMentis.Count > 0;
    
    /// <summary>
    /// Update hover state based on screen pixel mouse position.
    /// Returns true if hover state changed.
    /// </summary>
    public bool UpdateHover(float screenX, float screenY, Vector2i windowSize, float cellPixelSize)
    {
        if (!IsVisible)
            return false;
        
        int? newHoveredIndex = GetModusMentisIndexAtPosition(screenX, screenY, windowSize, cellPixelSize);
        
        if (newHoveredIndex != _hoveredModusMentisIndex)
        {
            _hoveredModusMentisIndex = newHoveredIndex;
            Render();
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get the modusMentis index at the given screen pixel position, or null if none.
    /// </summary>
    private int? GetModusMentisIndexAtPosition(float screenX, float screenY, Vector2i windowSize, float cellPixelSize)
    {
        // Get popup screen bounds
        var bounds = _popup.GetScreenBounds(windowSize);
        if (bounds == null)
        {
            Console.WriteLine($"TerminalThinkingModusMentisPopup: GetScreenBounds returned null");
            return null;
        }
        
        var (left, top, right, bottom) = bounds.Value;
        Console.WriteLine($"TerminalThinkingModusMentisPopup: Click at ({screenX}, {screenY}), popup bounds: ({left}, {top}, {right}, {bottom})");
        
        // Check if position is inside popup bounds
        if (screenX < left || screenX > right || screenY < top || screenY > bottom)
        {
            Console.WriteLine($"TerminalThinkingModusMentisPopup: Click OUTSIDE popup bounds");
            return null;
        }
        
        // Convert screen position to popup-local cell coordinates
        // Note: Cells are positioned by their centers, so we add half a cell size before dividing
        float relativeX = screenX - left;
        float relativeY = screenY - top;
        int cellX = (int)Math.Floor((relativeX + cellPixelSize * 0.5f) / cellPixelSize);
        int cellY = (int)Math.Floor((relativeY + cellPixelSize * 0.5f) / cellPixelSize);
        
        // Check if we're in the modusMentis list area (row 1 to MAX_VISIBLE_SKILLS+1, within popup width)
        if (cellY < 1 || cellY > MAX_VISIBLE_SKILLS || cellX < 0 || cellX >= POPUP_WIDTH)
            return null;
        
        // Calculate modusMentis index (accounting for scroll)
        int modusMentisIndex = (cellY - 1) + _scrollOffset;
        
        // Validate modusMentis index
        if (modusMentisIndex >= 0 && modusMentisIndex < _thinkingModiMentis.Count)
            return modusMentisIndex;
        
        return null;
    }
    
    /// <summary>
    /// Handle click at the given screen pixel position.
    /// Returns the selected modusMentis, or null if clicked outside.
    /// </summary>
    public ModusMentis? HandleClick(float screenX, float screenY, Vector2i windowSize, float cellPixelSize)
    {
        if (!IsVisible)
            return null;
        
        // Check if clicked inside popup
        int? modusMentisIndex = GetModusMentisIndexAtPosition(screenX, screenY, windowSize, cellPixelSize);
        
        if (modusMentisIndex.HasValue)
        {
            // Clicked on a modusMentis - return it
            var selectedModusMentis = _thinkingModiMentis[modusMentisIndex.Value];
            Hide();
            return selectedModusMentis;
        }
        else
        {
            // Clicked outside popup - close it
            Hide();
            return null;
        }
    }
    
    /// <summary>
    /// Render the popup with current state.
    /// </summary>
    private void Render()
    {
        _popup.Clear();
        
        if (!IsVisible)
            return;
        
        int visibleModusMentisCount = Math.Min(MAX_VISIBLE_SKILLS, _thinkingModiMentis.Count);
        int popupHeight = visibleModusMentisCount + 3; // Title + border + modiMentis + close hint
        
        // Draw background
        _popup.Fill(0, 0, POPUP_WIDTH, popupHeight, ' ', Config.ThinkingModusMentisPopup.ModusMentisNormalColor, Config.ThinkingModusMentisPopup.BackgroundColor);
        
        // Draw border
        _popup.DrawBox(0, 0, POPUP_WIDTH, popupHeight, BorderColor, Config.ThinkingModusMentisPopup.BackgroundColor);
        
        // Draw title
        int titleX = (POPUP_WIDTH - _title.Length) / 2;
        _popup.DrawText(titleX, 0, _title, TitleColor, Config.ThinkingModusMentisPopup.BackgroundColor);
        
        // Draw modiMentis
        int startIndex = _scrollOffset;
        int endIndex = Math.Min(startIndex + visibleModusMentisCount, _thinkingModiMentis.Count);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            int displayRow = i - startIndex + 1;
            var modusMentis = _thinkingModiMentis[i];
            
            bool isHovered = _hoveredModusMentisIndex == i;
            Vector4 textColor = isHovered ? Config.ThinkingModusMentisPopup.ModusMentisHoverColor : Config.ThinkingModusMentisPopup.ModusMentisNormalColor;
            Vector4 bgColor = isHovered ? Config.ThinkingModusMentisPopup.ModusMentisHoverBackgroundColor : Config.ThinkingModusMentisPopup.BackgroundColor;
            
            // Draw modusMentis name with arrow prefix
            string prefix = isHovered ? "> " : "  ";
            string displayText = prefix + modusMentis.DisplayName;
            
            // Truncate if too long
            int maxTextWidth = POPUP_WIDTH - 4;
            if (displayText.Length > maxTextWidth)
            {
                displayText = displayText.Substring(0, maxTextWidth - 3) + "...";
            }
            
            // Fill entire row with background
            _popup.Fill(1, displayRow, POPUP_WIDTH - 2, 1, ' ', textColor, bgColor);
            
            // Draw text
            _popup.DrawText(2, displayRow, displayText, textColor, bgColor);
        }
        
        // Draw close hint at bottom
        string closeHint = "[ESC or Click to close]";
        int hintX = (POPUP_WIDTH - closeHint.Length) / 2;
        _popup.DrawText(hintX, popupHeight - 1, closeHint, 
            Config.NarrativeUI.HintTextColor, Config.ThinkingModusMentisPopup.BackgroundColor);
    }
    
    /// <summary>
    /// Update hover by modusMentis index (called by controller that tracks popup bounds).
    /// </summary>
    public void SetHoveredModusMentis(int? modusMentisIndex)
    {
        if (_hoveredModusMentisIndex != modusMentisIndex)
        {
            _hoveredModusMentisIndex = modusMentisIndex;
            Render();
        }
    }
    
    /// <summary>
    /// Get the currently hovered modusMentis index.
    /// </summary>
    public int? GetHoveredModusMentisIndex() => _hoveredModusMentisIndex;
    
    /// <summary>
    /// Get the list of displayed modiMentis.
    /// </summary>
    public IReadOnlyList<ModusMentis> GetModiMentis() => _thinkingModiMentis.AsReadOnly();
    
    /// <summary>
    /// Get the fixed position where popup is displayed.
    /// </summary>
    public Vector2 GetFixedPosition() => _fixedPosition;
}
