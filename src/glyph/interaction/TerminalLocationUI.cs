using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game;

namespace Cathedral.Glyph.Interaction
{
    /// <summary>
    /// Helper structure for tracking clickable regions of actions in the terminal
    /// </summary>
    public record ActionRegion(
        int ActionIndex,
        int StartY,
        int EndY,
        int StartX,
        int EndX
    );

    /// <summary>
    /// Manages the terminal UI for location interactions.
    /// Handles rendering location information, action menus, and mouse interaction.
    /// Terminal size: 100x30 (larger than default to accommodate more text)
    /// </summary>
    public class TerminalLocationUI
    {
        // Use centralized configuration
        private const int TERMINAL_WIDTH = Config.LocationUI.TerminalWidth;
        private const int TERMINAL_HEIGHT = Config.LocationUI.TerminalHeight;
        private const int HEADER_HEIGHT = Config.LocationUI.HeaderHeight;
        private const int STATUS_BAR_HEIGHT = Config.LocationUI.StatusBarHeight;
        private const int ACTION_MENU_START_Y = Config.LocationUI.ActionMenuStartY;
        private const int NARRATIVE_START_Y = Config.LocationUI.NarrativeStartY;
        private const int NARRATIVE_HEIGHT = Config.LocationUI.NarrativeHeight;
        
        private readonly TerminalHUD _terminal;
        private List<ActionRegion> _actionRegions = new();
        private int? _hoveredActionIndex = null;
        
        public TerminalLocationUI(TerminalHUD terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            
            if (_terminal.Width != TERMINAL_WIDTH || _terminal.Height != TERMINAL_HEIGHT)
            {
                throw new ArgumentException($"Terminal must be {TERMINAL_WIDTH}x{TERMINAL_HEIGHT}, but got {_terminal.Width}x{_terminal.Height}");
            }
            
            Console.WriteLine($"TerminalLocationUI: Initialized with {TERMINAL_WIDTH}x{TERMINAL_HEIGHT} terminal");
        }
        
        /// <summary>
        /// Gets the dimensions expected for the terminal
        /// </summary>
        public static (int width, int height) GetRequiredDimensions() => (TERMINAL_WIDTH, TERMINAL_HEIGHT);
        
        /// <summary>
        /// Clears the entire terminal
        /// </summary>
        public void Clear()
        {
            for (int y = 0; y < TERMINAL_HEIGHT; y++)
            {
                for (int x = 0; x < TERMINAL_WIDTH; x++)
                {
                    _terminal.SetCell(x, y, ' ', Config.LocationUI.NarrativeColor, Config.LocationUI.BackgroundColor);
                }
            }
            _actionRegions.Clear();
            _hoveredActionIndex = null;
        }
        
        /// <summary>
        /// Renders the location header with name, turn count, and status info
        /// </summary>
        public void RenderLocationHeader(string locationName, string sublocation, int turnCount, string timeOfDay = "", string weather = "")
        {
            // Line 0: Location name (centered, bold)
            string title = $"=== {locationName} ===";
            int titleX = (TERMINAL_WIDTH - title.Length) / 2;
        _terminal.Text(titleX, 0, title, Config.LocationUI.HeaderColor, Config.LocationUI.BackgroundColor);
        
        // Line 1: Sublocation and turn info
        string sublocInfo = $"{sublocation}";
        string turnInfo = $"Turn {turnCount}";
        _terminal.Text(2, 1, sublocInfo, Config.LocationUI.NarrativeColor, Config.LocationUI.BackgroundColor);
        _terminal.Text(TERMINAL_WIDTH - turnInfo.Length - 2, 1, turnInfo, Config.LocationUI.NarrativeColor, Config.LocationUI.BackgroundColor);
            {
                string envInfo = "";
                if (!string.IsNullOrEmpty(timeOfDay)) envInfo += timeOfDay;
                if (!string.IsNullOrEmpty(weather))
                {
                    if (envInfo.Length > 0) envInfo += " | ";
                    envInfo += weather;
                }
                _terminal.Text(2, 2, envInfo, Config.LocationUI.NarrativeColor, Config.LocationUI.BackgroundColor);
            }
            
            // Separator line
            DrawHorizontalLine(HEADER_HEIGHT);
        }
        
        /// <summary>
        /// Renders the narrative text (location description and/or action result)
        /// </summary>
        public void RenderNarrative(string narrative)
        {
            if (string.IsNullOrEmpty(narrative))
                return;
            
            // Wrap text to fit terminal width (with margins)
            int maxWidth = TERMINAL_WIDTH - 4; // 2-char margins on each side
            List<string> wrappedLines = WrapText(narrative, maxWidth);
            
            // Render lines in narrative section
            int y = NARRATIVE_START_Y;
            int maxY = ACTION_MENU_START_Y - 1;
            
            foreach (var line in wrappedLines)
            {
                if (y >= maxY) break; // Don't overflow into action menu
                
                _terminal.Text(2, y, line, Config.LocationUI.NarrativeColor, Config.LocationUI.BackgroundColor);
                y++;
            }
        }
        
        /// <summary>
        /// Clears the action menu area (used during loading)
        /// </summary>
        public void ClearActionMenu()
        {
            _actionRegions.Clear();
            _hoveredActionIndex = null;
            
            // Clear action menu area
            for (int y = ACTION_MENU_START_Y - 1; y < TERMINAL_HEIGHT; y++)
            {
                for (int x = 0; x < TERMINAL_WIDTH; x++)
                {
                    _terminal.SetCell(x, y, ' ', Config.LocationUI.NarrativeColor, Config.LocationUI.BackgroundColor);
                }
            }
        }
        
        /// <summary>
        /// Renders the action menu with mouse-hover support
        /// </summary>
        public void RenderActionMenu(List<ActionInfo> actions, int? hoveredIndex = null)
        {
            _actionRegions.Clear();
            
            if (actions == null || actions.Count == 0)
            {
                _terminal.Text(2, ACTION_MENU_START_Y, "No actions available.", Config.LocationUI.StatusBarColor, Config.LocationUI.BackgroundColor);
                return;
            }
            
            // Draw separator before actions
            DrawHorizontalLine(ACTION_MENU_START_Y - 1);
            
            int y = ACTION_MENU_START_Y;
            int maxY = TERMINAL_HEIGHT - STATUS_BAR_HEIGHT - 1;
            
            for (int i = 0; i < actions.Count && y < maxY; i++)
            {
                ActionInfo actionInfo = actions[i];
                string displayText = actionInfo.GetFormattedDisplayText();
                bool isHovered = hoveredIndex.HasValue && hoveredIndex.Value == i;
                Vector4 color = isHovered ? Config.LocationUI.ActionHoverColor : Config.LocationUI.ActionNormalColor;
                
                // No number prefix, just the formatted text
                int maxActionWidth = TERMINAL_WIDTH - 4; // Just margins
                
                // Wrap action text if needed
                List<string> actionLines = WrapText(displayText, maxActionWidth);
                
                int startY = y;
                
                // Render all lines
                for (int lineIdx = 0; lineIdx < actionLines.Count && y < maxY; lineIdx++)
                {
                    _terminal.Text(2, y, actionLines[lineIdx], color, Config.LocationUI.BackgroundColor);
                    y++;
                }
                
                // Track clickable region
                _actionRegions.Add(new ActionRegion(i, startY, y - 1, 2, TERMINAL_WIDTH - 2));
                
                // Add spacing between actions (if room)
                if (i < actions.Count - 1 && y < maxY)
                {
                    y++;
                }
            }
        }
        
        /// <summary>
        /// Renders the status bar at the bottom
        /// </summary>
        public void RenderStatusBar(string message = "")
        {
            int statusY = TERMINAL_HEIGHT - 1;
            DrawHorizontalLine(statusY - 1);
            
            if (string.IsNullOrEmpty(message))
            {
                message = "Hover over actions with mouse | Click to select | ESC to return to world";
            }
            
            // Truncate if too long
            if (message.Length > TERMINAL_WIDTH - 4)
            {
                message = message.Substring(0, TERMINAL_WIDTH - 7) + "...";
            }
            
            _terminal.Text(2, statusY, message, Config.LocationUI.StatusBarColor, Config.LocationUI.BackgroundColor);
        }
        
        /// <summary>
        /// Gets the action index that is currently under the mouse cursor
        /// Returns null if no action is hovered
        /// </summary>
        public int? GetHoveredAction(int mouseX, int mouseY)
        {
            foreach (var region in _actionRegions)
            {
                if (mouseY >= region.StartY && mouseY <= region.EndY &&
                    mouseX >= region.StartX && mouseX <= region.EndX)
                {
                    return region.ActionIndex;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Updates hover state based on mouse position and re-renders actions if needed
        /// </summary>
        public void UpdateHover(int mouseX, int mouseY, List<ActionInfo> actions)
        {
            int? newHoveredIndex = GetHoveredAction(mouseX, mouseY);
            
            if (newHoveredIndex != _hoveredActionIndex)
            {
                _hoveredActionIndex = newHoveredIndex;
                
                // Re-render action menu with new hover state
                RenderActionMenu(actions, _hoveredActionIndex);
            }
        }
        
        /// <summary>
        /// Wraps text to fit within a maximum width, breaking at word boundaries
        /// </summary>
        private List<string> WrapText(string text, int maxWidth)
        {
            var lines = new List<string>();
            
            if (string.IsNullOrEmpty(text))
                return lines;
            
            if (maxWidth <= 0)
                maxWidth = TERMINAL_WIDTH - 4;
            
            // Split into paragraphs first (preserve intentional line breaks)
            string[] paragraphs = text.Split(new[] { '\n', '\r' }, StringSplitOptions.None);
            
            foreach (string paragraph in paragraphs)
            {
                if (string.IsNullOrWhiteSpace(paragraph))
                {
                    lines.Add(""); // Preserve empty lines
                    continue;
                }
                
                string[] words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                StringBuilder currentLine = new StringBuilder();
                
                foreach (string word in words)
                {
                    // Check if adding this word would exceed max width
                    string testLine = currentLine.Length == 0 ? word : currentLine + " " + word;
                    
                    if (testLine.Length <= maxWidth)
                    {
                        if (currentLine.Length > 0)
                            currentLine.Append(' ');
                        currentLine.Append(word);
                    }
                    else
                    {
                        // Word doesn't fit, start new line
                        if (currentLine.Length > 0)
                        {
                            lines.Add(currentLine.ToString());
                            currentLine.Clear();
                        }
                        
                        // Handle very long words that exceed maxWidth
                        if (word.Length > maxWidth)
                        {
                            // Split word across lines
                            int offset = 0;
                            while (offset < word.Length)
                            {
                                int length = Math.Min(maxWidth, word.Length - offset);
                                lines.Add(word.Substring(offset, length));
                                offset += length;
                            }
                        }
                        else
                        {
                            currentLine.Append(word);
                        }
                    }
                }
                
                // Add remaining text
                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine.ToString());
                }
            }
            
            return lines;
        }
        
        /// <summary>
        /// Draws a horizontal separator line
        /// </summary>
        private void DrawHorizontalLine(int y)
        {
            if (y < 0 || y >= TERMINAL_HEIGHT)
                return;
            
            for (int x = 0; x < TERMINAL_WIDTH; x++)
            {
                _terminal.SetCell(x, y, '─', Config.LocationUI.StatusBarColor, Config.LocationUI.BackgroundColor);
            }
        }
        
        private static readonly string[] LoadingFrames = new[]
        {
            "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"  // Braille spinner
        };
        
        private int _loadingFrameIndex = 0;
        private DateTime _lastFrameUpdate = DateTime.Now;
        
        /// <summary>
        /// Shows an animated loading indicator while waiting for LLM response
        /// Call repeatedly to animate (e.g., every frame)
        /// </summary>
        public void ShowLoadingIndicator(string message = "Thinking...")
        {
            Vector4 loadingColor = new Vector4(0.8f, 0.8f, 0.0f, 1.0f); // Yellow
            Vector4 spinnerColor = new Vector4(1.0f, 1.0f, 0.0f, 1.0f); // Bright yellow
            
            // Update animation frame every 100ms
            if ((DateTime.Now - _lastFrameUpdate).TotalMilliseconds > 100)
            {
                _loadingFrameIndex = (_loadingFrameIndex + 1) % LoadingFrames.Length;
                _lastFrameUpdate = DateTime.Now;
            }
            
            string spinner = LoadingFrames[_loadingFrameIndex];
            
            // Clear action menu to prevent clicks on old actions
            ClearActionMenu();
            
            // Clear narrative area
            for (int yPos = NARRATIVE_START_Y; yPos < ACTION_MENU_START_Y - 1; yPos++)
            {
                for (int xPos = 0; xPos < TERMINAL_WIDTH; xPos++)
                {
                    _terminal.SetCell(xPos, yPos, ' ', Config.LocationUI.NarrativeColor, Config.LocationUI.BackgroundColor);
                }
            }
            
            // Show animated spinner and message
            string loadingText = $"{spinner}  {message}  {spinner}";
            int loadingY = NARRATIVE_START_Y + NARRATIVE_HEIGHT / 2;
            int loadingX = (TERMINAL_WIDTH - loadingText.Length) / 2;
            _terminal.Text(loadingX, loadingY, loadingText, loadingColor, Config.LocationUI.BackgroundColor);
            
            // Add animated dots below
            string dots = new string('.', (_loadingFrameIndex % 4));
            string space = new string(' ', (_loadingFrameIndex % 4));
            string hint = $"{space}Please wait{dots}";
            int hintY = loadingY + 2;
            int hintX = (TERMINAL_WIDTH - hint.Length) / 2;
            _terminal.Text(hintX, hintY, hint, Config.LocationUI.StatusBarColor, Config.LocationUI.BackgroundColor);
            
            // Add progress indicator (alternating bars)
            int barWidth = 30;
            int barY = loadingY - 2;
            int barX = (TERMINAL_WIDTH - barWidth) / 2;
            string progressBar = GenerateProgressBar(barWidth, _loadingFrameIndex);
            _terminal.Text(barX, barY, progressBar, spinnerColor, Config.LocationUI.BackgroundColor);
        }
        
        /// <summary>
        /// Generates an animated progress bar
        /// </summary>
        private string GenerateProgressBar(int width, int frame)
        {
            var bar = new System.Text.StringBuilder();
            bar.Append('[');
            
            for (int i = 0; i < width - 2; i++)
            {
                // Create a moving wave effect
                int pos = (frame + i) % 8;
                if (pos < 2)
                    bar.Append('█');
                else if (pos < 4)
                    bar.Append('▓');
                else if (pos < 6)
                    bar.Append('▒');
                else
                    bar.Append('░');
            }
            
            bar.Append(']');
            return bar.ToString();
        }
        
        /// <summary>
        /// Shows a result message (success or failure) temporarily in the narrative area
        /// </summary>
        public void ShowResultMessage(string message, bool success)
        {
            Vector4 color = success ? new Vector4(0.0f, 1.0f, 0.0f, 1.0f) : new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
            
            // Clear narrative area
            for (int y = NARRATIVE_START_Y; y < ACTION_MENU_START_Y - 1; y++)
            {
                for (int x = 0; x < TERMINAL_WIDTH; x++)
                {
                    _terminal.SetCell(x, y, ' ', Config.LocationUI.NarrativeColor, Config.LocationUI.BackgroundColor);
                }
            }
            
            // Show message centered
            List<string> wrappedLines = WrapText(message, TERMINAL_WIDTH - 4);
            int startY = NARRATIVE_START_Y + (NARRATIVE_HEIGHT - wrappedLines.Count) / 2;
            
            for (int i = 0; i < wrappedLines.Count; i++)
            {
                int y = startY + i;
                if (y >= NARRATIVE_START_Y && y < ACTION_MENU_START_Y - 1)
                {
                    string line = wrappedLines[i];
                    int x = (TERMINAL_WIDTH - line.Length) / 2;
                    _terminal.Text(x, y, line, color, Config.LocationUI.BackgroundColor);
                }
            }
            
            // Add instruction to click to continue
            string instruction = success ? "(Click anywhere to continue)" : "(Click anywhere to exit)";
            int instructionY = ACTION_MENU_START_Y - 2;
            int instructionX = (TERMINAL_WIDTH - instruction.Length) / 2;
            _terminal.Text(instructionX, instructionY, instruction, Config.LocationUI.StatusBarColor, Config.LocationUI.BackgroundColor);
        }
        
        /// <summary>
        /// Renders a complete location UI frame with all sections
        /// </summary>
        public void RenderComplete(
            string locationName,
            string sublocation,
            int turnCount,
            string narrative,
            List<ActionInfo> actions,
            string timeOfDay = "",
            string weather = "",
            string statusMessage = "")
        {
            Clear();
            RenderLocationHeader(locationName, sublocation, turnCount, timeOfDay, weather);
            RenderNarrative(narrative);
            RenderActionMenu(actions, _hoveredActionIndex);
            RenderStatusBar(statusMessage);
        }
    }
}
