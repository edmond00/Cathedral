using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// Terminal-based popup for selecting thinking skills
/// Appears at click position, shows scrollable skill list with keyboard navigation
/// </summary>
public class TerminalThinkingSkillPopup
{
    private readonly PopupTerminalHUD _popup;
    private readonly List<Skill> _skills;
    private int _selectedIndex = 0;
    private int _scrollOffset = 0;
    private bool _isVisible = false;
    private Vector2 _fixedPosition;

    private const int PopupWidth = 35;
    private const int PopupMaxHeight = 22;
    private const int PopupHeaderHeight = 2;
    
    // Colors
    private readonly Vector4 HeaderColor = new(0.0f, 0.8f, 1.0f, 1.0f);        // Cyan
    private readonly Vector4 NormalColor = new(0.9f, 0.9f, 0.9f, 1.0f);        // Light gray
    private readonly Vector4 SelectedColor = new(1.0f, 1.0f, 0.0f, 1.0f);      // Yellow
    private readonly Vector4 DisabledColor = new(0.5f, 0.5f, 0.5f, 1.0f);      // Gray

    public TerminalThinkingSkillPopup(PopupTerminalHUD popup, List<Skill> thinkingSkills)
    {
        _popup = popup;
        _skills = thinkingSkills;
    }

    public bool IsVisible => _isVisible;
    public Skill? SelectedSkill => _selectedIndex >= 0 && _selectedIndex < _skills.Count 
        ? _skills[_selectedIndex] 
        : null;

    /// <summary>
    /// Show popup at specified position (fixed, not following mouse)
    /// </summary>
    public void Show(Vector2 position)
    {
        _isVisible = true;
        _selectedIndex = 0;
        _scrollOffset = 0;
        _fixedPosition = position;
        
        // Keep popup on screen
        float maxX = 1920 - (PopupWidth * 12); // Assuming 12 pixels per char width
        float maxY = 1080 - (PopupMaxHeight * 20); // Assuming 20 pixels per char height
        _fixedPosition.X = Math.Clamp(_fixedPosition.X, 0, maxX);
        _fixedPosition.Y = Math.Clamp(_fixedPosition.Y, 0, maxY);
    }

    /// <summary>
    /// Hide popup
    /// </summary>
    public void Hide()
    {
        _isVisible = false;
    }

    /// <summary>
    /// Handle keyboard input (arrow keys, enter, escape)
    /// Returns: selected skill if confirmed, null if cancelled or still selecting
    /// </summary>
    public Skill? HandleInput(ConsoleKey key)
    {
        if (!_isVisible) return null;

        switch (key)
        {
            case ConsoleKey.UpArrow:
                _selectedIndex = Math.Max(0, _selectedIndex - 1);
                AdjustScrollOffset();
                return null;

            case ConsoleKey.DownArrow:
                _selectedIndex = Math.Min(_skills.Count - 1, _selectedIndex + 1);
                AdjustScrollOffset();
                return null;

            case ConsoleKey.Enter:
                Hide();
                return SelectedSkill;

            case ConsoleKey.Escape:
                Hide();
                return null;

            default:
                // Number keys 1-9 for quick selection
                if (key >= ConsoleKey.D1 && key <= ConsoleKey.D9)
                {
                    int index = (int)key - (int)ConsoleKey.D1;
                    if (index < _skills.Count)
                    {
                        _selectedIndex = index;
                        Hide();
                        return SelectedSkill;
                    }
                }
                return null;
        }
    }

    /// <summary>
    /// Handle mouse click on skill (returns selected skill if clicked)
    /// </summary>
    public Skill? HandleMouseClick(int mouseX, int mouseY)
    {
        if (!_isVisible) return null;

        // Convert screen coordinates to popup-local coordinates
        // This is approximate - actual conversion depends on popup positioning
        int localY = mouseY - 2; // Skip header
        
        if (localY >= 0 && localY < GetVisibleSkillCount())
        {
            int skillIndex = _scrollOffset + localY;
            if (skillIndex >= 0 && skillIndex < _skills.Count)
            {
                _selectedIndex = skillIndex;
                Hide();
                return SelectedSkill;
            }
        }

        return null;
    }

    /// <summary>
    /// Render popup
    /// </summary>
    public void Render()
    {
        if (!_isVisible) return;

        _popup.Clear();
        _popup.SetMousePosition(_fixedPosition);

        // Header
        string title = "Select Thinking Skill";
        _popup.DrawCenteredText(0, title, new Vector4(0.0f, 0.8f, 1.0f, 1.0f));
        _popup.DrawText(0, 1, new string('─', PopupWidth), new Vector4(0.0f, 0.8f, 1.0f, 1.0f));

        // Skills list (scrollable)
        int visibleCount = GetVisibleSkillCount();
        int y = PopupHeaderHeight;

        for (int i = 0; i < visibleCount && (_scrollOffset + i) < _skills.Count; i++)
        {
            int skillIndex = _scrollOffset + i;
            var skill = _skills[skillIndex];
            bool isSelected = skillIndex == _selectedIndex;

            string prefix = isSelected ? "> " : "  ";
            string skillText = $"{prefix}{skill.DisplayName}";
            
            // Truncate if too long
            if (skillText.Length > PopupWidth - 2)
            {
                skillText = skillText.Substring(0, PopupWidth - 5) + "...";
            }

            var color = isSelected ? new Vector4(1.0f, 1.0f, 0.0f, 1.0f) : new Vector4(0.9f, 0.9f, 0.9f, 1.0f);
            _popup.DrawText(1, y, skillText, color);
            y++;
        }

        // Footer with instructions
        if (_skills.Count > visibleCount)
        {
            string scrollInfo = $"({_scrollOffset + 1}-{Math.Min(_scrollOffset + visibleCount, _skills.Count)} of {_skills.Count})";
            _popup.DrawText(1, y, scrollInfo, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
            y++;
        }

        string instructions = "[↑↓] Navigate [Enter] Select [ESC] Cancel";
        if (instructions.Length > PopupWidth)
        {
            instructions = "[Enter] OK [ESC] Cancel";
        }
        _popup.DrawText(1, y, instructions, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
    }

    private int GetVisibleSkillCount()
    {
        int availableLines = PopupMaxHeight - PopupHeaderHeight - 2; // -2 for footer
        return Math.Min(availableLines, _skills.Count);
    }

    private void AdjustScrollOffset()
    {
        int visibleCount = GetVisibleSkillCount();
        
        // Scroll up if selected item is above visible area
        if (_selectedIndex < _scrollOffset)
        {
            _scrollOffset = _selectedIndex;
        }
        // Scroll down if selected item is below visible area
        else if (_selectedIndex >= _scrollOffset + visibleCount)
        {
            _scrollOffset = _selectedIndex - visibleCount + 1;
        }
    }
}
