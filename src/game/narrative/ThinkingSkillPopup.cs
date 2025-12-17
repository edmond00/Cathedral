using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// UI popup that displays a list of thinking skills at the mouse position.
/// Returns the selected skill or null if cancelled with ESC.
/// </summary>
public class ThinkingSkillPopup
{
    private readonly List<Skill> _thinkingSkills;
    private int _selectedIndex = 0;
    private bool _isVisible = false;

    public ThinkingSkillPopup(List<Skill> thinkingSkills)
    {
        _thinkingSkills = thinkingSkills;
    }

    /// <summary>
    /// Shows the popup at the specified mouse position.
    /// </summary>
    public void Show(int mouseX, int mouseY)
    {
        _isVisible = true;
        _selectedIndex = 0;
        MouseX = mouseX;
        MouseY = mouseY;
    }

    /// <summary>
    /// Hides the popup.
    /// </summary>
    public void Hide()
    {
        _isVisible = false;
    }

    /// <summary>
    /// Handles keyboard input for navigation and selection.
    /// Returns the selected skill if Enter was pressed, null otherwise.
    /// </summary>
    public Skill? HandleInput(ConsoleKeyInfo keyInfo)
    {
        if (!_isVisible)
        {
            return null;
        }

        switch (keyInfo.Key)
        {
            case ConsoleKey.UpArrow:
                _selectedIndex = (_selectedIndex - 1 + _thinkingSkills.Count) % _thinkingSkills.Count;
                break;

            case ConsoleKey.DownArrow:
                _selectedIndex = (_selectedIndex + 1) % _thinkingSkills.Count;
                break;

            case ConsoleKey.Enter:
                var selected = _thinkingSkills[_selectedIndex];
                Hide();
                return selected;

            case ConsoleKey.Escape:
                Hide();
                return null;
        }

        return null;
    }

    /// <summary>
    /// Renders the popup to the terminal.
    /// Shows all thinking skills with the selected one highlighted.
    /// </summary>
    public void Render(Action<int, int, string, ConsoleColor> writeAt)
    {
        if (!_isVisible)
        {
            return;
        }

        int x = MouseX;
        int y = MouseY;

        // Draw border
        int width = _thinkingSkills.Max(s => s.DisplayName.Length) + 4;
        int height = _thinkingSkills.Count + 2;

        // Draw top border
        writeAt(x, y, "┌" + new string('─', width - 2) + "┐", ConsoleColor.White);

        // Draw skills
        for (int i = 0; i < _thinkingSkills.Count; i++)
        {
            var skill = _thinkingSkills[i];
            bool isSelected = (i == _selectedIndex);
            
            string prefix = isSelected ? "► " : "  ";
            string text = prefix + skill.DisplayName;
            var color = isSelected ? ConsoleColor.Yellow : ConsoleColor.Gray;

            writeAt(x, y + 1 + i, "│" + text.PadRight(width - 2) + "│", color);
        }

        // Draw bottom border
        writeAt(x, y + height - 1, "└" + new string('─', width - 2) + "┘", ConsoleColor.White);
    }

    public int MouseX { get; private set; }
    public int MouseY { get; private set; }
    public bool IsVisible => _isVisible;
    public int SkillCount => _thinkingSkills.Count;
}
