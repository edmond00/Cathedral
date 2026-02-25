using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Terminal;

namespace Cathedral.Game;

/// <summary>
/// Renders and manages the main menu UI on the TerminalHUD.
/// Displays centered, hoverable/clickable buttons (New, Continue, Exit).
/// Isolated from the narrative system — only active during GameMode.MainMenu.
/// </summary>
public class MainMenuRenderer
{
    private readonly TerminalHUD _terminal;
    private readonly List<MenuButton> _buttons = new();
    private int _hoveredIndex = -1;

    // Layout constants
    private const int TitleRow = 35;
    private const int FirstButtonRow = 42;
    private const int ButtonSpacing = 3;
    private const int ButtonWidth = 20;

    /// <summary>
    /// Whether a game session has been started (New or Continue clicked at least once).
    /// </summary>
    public bool HasGameStarted { get; set; } = false;

    public MainMenuRenderer(TerminalHUD terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }

    /// <summary>
    /// Configures the menu buttons. Call before Render().
    /// </summary>
    public void SetButtons(Action onNew, Action onContinue, Action onProtagonist, Action onExit)
    {
        _buttons.Clear();
        _buttons.Add(new MenuButton("New", onNew, true));
        _buttons.Add(new MenuButton("Continue", onContinue, HasGameStarted));
        _buttons.Add(new MenuButton("Protagonist", onProtagonist, HasGameStarted));
        _buttons.Add(new MenuButton("Exit", onExit, true));
    }

    /// <summary>
    /// Updates the enabled state of the Continue button.
    /// </summary>
    public void SetContinueEnabled(bool enabled)
    {
        if (_buttons.Count >= 2)
        {
            _buttons[1] = _buttons[1] with { Enabled = enabled };
        }
    }

    /// <summary>
    /// Updates the enabled state of the Protagonist button.
    /// </summary>
    public void SetProtagonistEnabled(bool enabled)
    {
        if (_buttons.Count >= 3)
        {
            _buttons[2] = _buttons[2] with { Enabled = enabled };
        }
    }

    /// <summary>
    /// Renders the full menu to the terminal.
    /// </summary>
    public void Render()
    {
        // Fill entire terminal with black
        _terminal.Fill(' ', Config.Colors.Black, Config.Colors.Black);
        _terminal.Visible = true;

        // Draw title
        _terminal.CenteredText(TitleRow, "C A T H E D R A L", Config.Colors.BrightYellow, Config.Colors.Black);
        _terminal.CenteredText(TitleRow + 2, "─────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);

        // Draw buttons
        for (int i = 0; i < _buttons.Count; i++)
        {
            DrawButton(i);
        }
    }

    /// <summary>
    /// Handles mouse movement. Updates hover state and redraws affected buttons.
    /// </summary>
    public void OnMouseMove(int x, int y)
    {
        int newHovered = GetButtonAtPosition(x, y);

        if (newHovered != _hoveredIndex)
        {
            int oldHovered = _hoveredIndex;
            _hoveredIndex = newHovered;

            // Redraw only affected buttons
            if (oldHovered >= 0 && oldHovered < _buttons.Count)
                DrawButton(oldHovered);
            if (newHovered >= 0 && newHovered < _buttons.Count)
                DrawButton(newHovered);
        }
    }

    /// <summary>
    /// Handles mouse click. Fires the callback of the clicked button if enabled.
    /// </summary>
    public void OnMouseClick(int x, int y)
    {
        int index = GetButtonAtPosition(x, y);
        if (index >= 0 && index < _buttons.Count && _buttons[index].Enabled)
        {
            _buttons[index].OnClick?.Invoke();
        }
    }

    // ── Private helpers ──────────────────────────────────────────

    private void DrawButton(int index)
    {
        if (index < 0 || index >= _buttons.Count) return;

        var button = _buttons[index];
        int row = FirstButtonRow + index * ButtonSpacing;
        int terminalWidth = _terminal.Width;
        int startX = (terminalWidth - ButtonWidth) / 2;

        bool isHovered = index == _hoveredIndex && button.Enabled;

        // Choose colors
        Vector4 textColor, bgColor;
        if (!button.Enabled)
        {
            textColor = Config.Colors.DarkGray35;
            bgColor = Config.Colors.Black;
        }
        else if (isHovered)
        {
            textColor = Config.Colors.BrightYellow;
            bgColor = Config.Colors.DarkYellow;
        }
        else
        {
            textColor = Config.Colors.White;
            bgColor = Config.Colors.Black;
        }

        // Clear the button row area
        _terminal.FillRect(startX, row, ButtonWidth, 1, ' ', textColor, bgColor);

        // Format label centered within button width: "[ Label ]"
        string label = $"[ {button.Label} ]";
        int labelX = startX + (ButtonWidth - label.Length) / 2;
        _terminal.Text(labelX, row, label, textColor, bgColor);
    }

    private int GetButtonAtPosition(int x, int y)
    {
        int terminalWidth = _terminal.Width;
        int startX = (terminalWidth - ButtonWidth) / 2;
        int endX = startX + ButtonWidth;

        if (x < startX || x >= endX) return -1;

        for (int i = 0; i < _buttons.Count; i++)
        {
            int row = FirstButtonRow + i * ButtonSpacing;
            if (y == row) return i;
        }

        return -1;
    }

    // ── Data types ───────────────────────────────────────────────

    private record struct MenuButton(string Label, Action? OnClick, bool Enabled);
}
