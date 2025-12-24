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
    
    // Colors
    private static readonly Vector4 BorderColor = new(0.5f, 0.9f, 1.0f, 1.0f); // Light cyan
    private static readonly Vector4 TitleColor = new(1.0f, 1.0f, 0.0f, 1.0f); // Yellow
    private static readonly Vector4 SkillNormalColor = new(0.9f, 0.9f, 0.9f, 1.0f); // Light gray
    private static readonly Vector4 SkillHoverColor = new(1.0f, 1.0f, 0.0f, 1.0f); // Yellow
    private static readonly Vector4 BackgroundColor = new(0.0f, 0.0f, 0.0f, 0.9f); // Semi-transparent black
    private static readonly Vector4 TransparentColor = new(0.0f, 0.0f, 0.0f, 0.0f);
    
    public TerminalThinkingSkillPopup(PopupTerminalHUD popup)
    {
        _popup = popup ?? throw new ArgumentNullException(nameof(popup));
    }
    
    /// <summary>
    /// Show the popup at a fixed screen position with the list of thinking skills.
    /// </summary>
    public void Show(Vector2 screenPosition, List<Skill> thinkingSkills)
    {
        _thinkingSkills = thinkingSkills ?? throw new ArgumentNullException(nameof(thinkingSkills));
        _fixedPosition = screenPosition;
        _hoveredSkillIndex = null;
        _scrollOffset = 0;
        
        // Set popup to fixed position (it will stay here until updated)
        _popup.SetMousePosition(_fixedPosition);
        
        Render();
    }
    
    /// <summary>
    /// Hide the popup.
    /// </summary>
    public void Hide()
    {
        _popup.Clear();
        _thinkingSkills.Clear();
        _hoveredSkillIndex = null;
    }
    
    /// <summary>
    /// Check if the popup is currently visible.
    /// </summary>
    public bool IsVisible => _thinkingSkills.Count > 0;
    
    /// <summary>
    /// Update hover state based on mouse position relative to popup.
    /// Returns true if hover state changed.
    /// </summary>
    public bool UpdateHover(int mouseX, int mouseY)
    {
        if (!IsVisible)
            return false;
        
        // Convert screen mouse position to popup-relative position
        // Note: This is approximate since we don't have exact popup pixel bounds
        // We'll use cell-based detection
        
        int? newHoveredIndex = GetSkillIndexAtPosition(mouseX, mouseY);
        
        if (newHoveredIndex != _hoveredSkillIndex)
        {
            _hoveredSkillIndex = newHoveredIndex;
            Render();
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Get the skill index at the given mouse position, or null if none.
    /// </summary>
    private int? GetSkillIndexAtPosition(int mouseX, int mouseY)
    {
        // This is a simplified version - we'd need more precise bounds
        // For now, we'll just return null (hover will be updated by the controller)
        // TODO: Implement proper popup bounds detection
        return null;
    }
    
    /// <summary>
    /// Handle click at the given position.
    /// Returns the selected skill, or null if clicked outside or on close.
    /// </summary>
    public Skill? HandleClick(int mouseX, int mouseY)
    {
        if (!IsVisible)
            return null;
        
        // For now, any click closes the popup
        // TODO: Implement actual skill selection
        Hide();
        return null;
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
        _popup.Fill(0, 0, POPUP_WIDTH, popupHeight, ' ', SkillNormalColor, BackgroundColor);
        
        // Draw border
        _popup.DrawBox(0, 0, POPUP_WIDTH, popupHeight, BorderColor, TransparentColor);
        
        // Draw title
        string title = "Select Thinking Skill";
        int titleX = (POPUP_WIDTH - title.Length) / 2;
        _popup.DrawText(titleX, 0, title, TitleColor, BackgroundColor);
        
        // Draw skills
        int startIndex = _scrollOffset;
        int endIndex = Math.Min(startIndex + visibleSkillCount, _thinkingSkills.Count);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            int displayRow = i - startIndex + 1;
            var skill = _thinkingSkills[i];
            
            bool isHovered = _hoveredSkillIndex == i;
            Vector4 textColor = isHovered ? SkillHoverColor : SkillNormalColor;
            Vector4 bgColor = isHovered ? new Vector4(0.2f, 0.2f, 0.0f, 0.9f) : BackgroundColor;
            
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
            new Vector4(0.5f, 0.5f, 0.5f, 1.0f), BackgroundColor);
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
