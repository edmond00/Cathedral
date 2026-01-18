using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// Terminal-based popup for selecting a thinking skill.
/// Fixed at click position, shows list of skills with hover highlighting.
/// </summary>
public class TerminalThinkingSkillPopup
{
    private const int POPUP_WIDTH = 35;
    private const int MAX_VISIBLE_SKILLS = 15;
    
    private readonly PopupTerminalHUD _popup;
    private List<Skill> _thinkingSkills = new();
    private int? _hoveredSkillIndex = null;
    private Vector2 _fixedPosition;
    private int _scrollOffset = 0;
    private string _title = "Select Thinking Skill";
    
    // Colors from centralized config
    private static readonly Vector4 BorderColor = Config.Colors.MediumGray60;
    private static readonly Vector4 TitleColor = Config.Colors.DarkYellowGrey;
    
    public TerminalThinkingSkillPopup(PopupTerminalHUD popup)
    {
        _popup = popup ?? throw new ArgumentNullException(nameof(popup));
    }
    
    /// <summary>
    /// Show the popup at a fixed screen position with the list of skills.
    /// </summary>
    /// <param name="screenPosition">Screen position to show popup at</param>
    /// <param name="skills">List of skills to display</param>
    /// <param name="title">Optional custom title (defaults to "Select Thinking Skill")</param>
    public void Show(Vector2 screenPosition, List<Skill> skills, string title = "Select Thinking Skill")
    {
        _thinkingSkills = skills ?? throw new ArgumentNullException(nameof(skills));
        _fixedPosition = screenPosition;
        _hoveredSkillIndex = null;
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
        _thinkingSkills.Clear();
        _hoveredSkillIndex = null;
    }
    
    /// <summary>
    /// Check if the popup is currently visible.
    /// </summary>
    public bool IsVisible => _thinkingSkills.Count > 0;
    
    /// <summary>
    /// Update hover state based on screen pixel mouse position.
    /// Returns true if hover state changed.
    /// </summary>
    public bool UpdateHover(float screenX, float screenY, Vector2i windowSize, int cellPixelSize)
    {
        if (!IsVisible)
            return false;
        
        int? newHoveredIndex = GetSkillIndexAtPosition(screenX, screenY, windowSize, cellPixelSize);
        
        if (newHoveredIndex != _hoveredSkillIndex)
        {
            _hoveredSkillIndex = newHoveredIndex;
            Render();
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get the skill index at the given screen pixel position, or null if none.
    /// </summary>
    private int? GetSkillIndexAtPosition(float screenX, float screenY, Vector2i windowSize, int cellPixelSize)
    {
        // Get popup screen bounds
        var bounds = _popup.GetScreenBounds(windowSize);
        if (bounds == null)
        {
            Console.WriteLine($"TerminalThinkingSkillPopup: GetScreenBounds returned null");
            return null;
        }
        
        var (left, top, right, bottom) = bounds.Value;
        Console.WriteLine($"TerminalThinkingSkillPopup: Click at ({screenX}, {screenY}), popup bounds: ({left}, {top}, {right}, {bottom})");
        
        // Check if position is inside popup bounds
        if (screenX < left || screenX > right || screenY < top || screenY > bottom)
        {
            Console.WriteLine($"TerminalThinkingSkillPopup: Click OUTSIDE popup bounds");
            return null;
        }
        
        // Convert screen position to popup-local cell coordinates
        // Note: Cells are positioned by their centers, so we add half a cell size before dividing
        float relativeX = screenX - left;
        float relativeY = screenY - top;
        int cellX = (int)Math.Floor((relativeX + cellPixelSize * 0.5f) / cellPixelSize);
        int cellY = (int)Math.Floor((relativeY + cellPixelSize * 0.5f) / cellPixelSize);
        
        // Check if we're in the skill list area (row 1 to MAX_VISIBLE_SKILLS+1, within popup width)
        if (cellY < 1 || cellY > MAX_VISIBLE_SKILLS || cellX < 0 || cellX >= POPUP_WIDTH)
            return null;
        
        // Calculate skill index (accounting for scroll)
        int skillIndex = (cellY - 1) + _scrollOffset;
        
        // Validate skill index
        if (skillIndex >= 0 && skillIndex < _thinkingSkills.Count)
            return skillIndex;
        
        return null;
    }
    
    /// <summary>
    /// Handle click at the given screen pixel position.
    /// Returns the selected skill, or null if clicked outside.
    /// </summary>
    public Skill? HandleClick(float screenX, float screenY, Vector2i windowSize, int cellPixelSize)
    {
        if (!IsVisible)
            return null;
        
        // Check if clicked inside popup
        int? skillIndex = GetSkillIndexAtPosition(screenX, screenY, windowSize, cellPixelSize);
        
        if (skillIndex.HasValue)
        {
            // Clicked on a skill - return it
            var selectedSkill = _thinkingSkills[skillIndex.Value];
            Hide();
            return selectedSkill;
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
        
        int visibleSkillCount = Math.Min(MAX_VISIBLE_SKILLS, _thinkingSkills.Count);
        int popupHeight = visibleSkillCount + 3; // Title + border + skills + close hint
        
        // Draw background
        _popup.Fill(0, 0, POPUP_WIDTH, popupHeight, ' ', Config.ThinkingSkillPopup.SkillNormalColor, Config.ThinkingSkillPopup.BackgroundColor);
        
        // Draw border
        _popup.DrawBox(0, 0, POPUP_WIDTH, popupHeight, BorderColor, Config.ThinkingSkillPopup.BackgroundColor);
        
        // Draw title
        int titleX = (POPUP_WIDTH - _title.Length) / 2;
        _popup.DrawText(titleX, 0, _title, TitleColor, Config.ThinkingSkillPopup.BackgroundColor);
        
        // Draw skills
        int startIndex = _scrollOffset;
        int endIndex = Math.Min(startIndex + visibleSkillCount, _thinkingSkills.Count);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            int displayRow = i - startIndex + 1;
            var skill = _thinkingSkills[i];
            
            bool isHovered = _hoveredSkillIndex == i;
            Vector4 textColor = isHovered ? Config.ThinkingSkillPopup.SkillHoverColor : Config.ThinkingSkillPopup.SkillNormalColor;
            Vector4 bgColor = isHovered ? Config.ThinkingSkillPopup.SkillHoverBackgroundColor : Config.ThinkingSkillPopup.BackgroundColor;
            
            // Draw skill name with arrow prefix
            string prefix = isHovered ? "> " : "  ";
            string displayText = prefix + skill.DisplayName;
            
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
            Config.NarrativeUI.HintTextColor, Config.ThinkingSkillPopup.BackgroundColor);
    }
    
    /// <summary>
    /// Update hover by skill index (called by controller that tracks popup bounds).
    /// </summary>
    public void SetHoveredSkill(int? skillIndex)
    {
        if (_hoveredSkillIndex != skillIndex)
        {
            _hoveredSkillIndex = skillIndex;
            Render();
        }
    }
    
    /// <summary>
    /// Get the currently hovered skill index.
    /// </summary>
    public int? GetHoveredSkillIndex() => _hoveredSkillIndex;
    
    /// <summary>
    /// Get the list of displayed skills.
    /// </summary>
    public IReadOnlyList<Skill> GetSkills() => _thinkingSkills.AsReadOnly();
    
    /// <summary>
    /// Get the fixed position where popup is displayed.
    /// </summary>
    public Vector2 GetFixedPosition() => _fixedPosition;
}
