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

    // Layout constants — title block
    private const int TitleOrnamentTopRow    = 28;
    private const int TitleMainRow           = 30;
    private const int TitleChapterRow        = 33;
    private const int TitleSubtitleRow       = 35;
    private const int TitleOrnamentBottomRow = 37;

    private const int FirstButtonRow = 42;
    private const int ButtonSpacing  = 3;
    private const int ButtonWidth    = 20;

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
    public void SetButtons(Action onNew, Action onContinue, Action onProtagonist, Action onSettings, Action onExit)
    {
        _buttons.Clear();
        _buttons.Add(new MenuButton("New", onNew, true));
        _buttons.Add(new MenuButton("Continue", onContinue, HasGameStarted));
        _buttons.Add(new MenuButton("Protagonist", onProtagonist, HasGameStarted));
        _buttons.Add(new MenuButton("Settings", onSettings, true));
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

        // ── Title block ──────────────────────────────────────────
        const string ornament = "─ · ─ · ─ · ─ · ─ · ─ · ─ · ─";
        _terminal.CenteredText(TitleOrnamentTopRow,    ornament,
            Config.Colors.DarkGray35,   Config.Colors.Black);
        _terminal.CenteredText(TitleMainRow,           Config.Name.GameTitle,
            Config.Colors.BrightYellow, Config.Colors.Black);
        _terminal.CenteredText(TitleChapterRow,        Spaced(Config.Name.Chapter.ToUpper()),
            Config.Colors.MediumGray50, Config.Colors.Black);
        _terminal.CenteredText(TitleSubtitleRow,       $"·  {Config.Name.ChapterSubtitle}  ·",
            Config.Colors.MediumGray60, Config.Colors.Black);
        _terminal.CenteredText(TitleOrnamentBottomRow, ornament,
            Config.Colors.DarkGray35,   Config.Colors.Black);

        // Draw buttons
        for (int i = 0; i < _buttons.Count; i++)
        {
            DrawButton(i);
        }

        // Edge rules against the sphere, drawn last so nothing overwrites them
        _terminal.DrawSideRails();
    }

    /// <summary>Spreads characters within each word with single spaces, and separates words with triple spaces.</summary>
    private static string Spaced(string s)
    {
        var parts = s.Split(' ');
        var sb = new System.Text.StringBuilder();
        for (int w = 0; w < parts.Length; w++)
        {
            if (w > 0) sb.Append("   ");
            for (int c = 0; c < parts[w].Length; c++)
            {
                if (c > 0) sb.Append(' ');
                sb.Append(parts[w][c]);
            }
        }
        return sb.ToString();
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

    public int GetButtonAtPosition(int x, int y)
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

    /// <summary>Returns the button index if enabled and under (x, y), otherwise -1.</summary>
    public int GetEnabledButtonAtPosition(int x, int y)
    {
        int idx = GetButtonAtPosition(x, y);
        return (idx >= 0 && idx < _buttons.Count && _buttons[idx].Enabled) ? idx : -1;
    }

    /// <summary>
    /// The menu buttons with their cell positions, so --cli can list and press them by name
    /// instead of hard-coding the layout constants.
    /// </summary>
    public IReadOnlyList<(string Label, bool Enabled, int X, int Y)> CliButtons()
    {
        int startX = (_terminal.Width - ButtonWidth) / 2;
        return _buttons
            .Select((b, i) => (b.Label, b.Enabled, startX, FirstButtonRow + i * ButtonSpacing))
            .ToList();
    }

    // ── Data types ───────────────────────────────────────────────

    private record struct MenuButton(string Label, Action? OnClick, bool Enabled);
}
