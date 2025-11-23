using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Mathematics;
using Cathedral.Terminal;

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
        // Terminal dimensions
        private const int TERMINAL_WIDTH = 100;
        private const int TERMINAL_HEIGHT = 30;
        
        // Layout constants
        private const int HEADER_HEIGHT = 3;
        private const int STATUS_BAR_HEIGHT = 1;
        private const int ACTION_MENU_START_Y = 18;
        private const int NARRATIVE_START_Y = HEADER_HEIGHT + 1;
        private const int NARRATIVE_HEIGHT = ACTION_MENU_START_Y - NARRATIVE_START_Y - 1;
        
        // Colors
        private static readonly Vector4 HeaderColor = new Vector4(0.0f, 0.8f, 1.0f, 1.0f); // Cyan
        private static readonly Vector4 NarrativeColor = new Vector4(0.9f, 0.9f, 0.9f, 1.0f); // Light gray
        private static readonly Vector4 ActionNormalColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f); // White
        private static readonly Vector4 ActionHoverColor = new Vector4(1.0f, 1.0f, 0.0f, 1.0f); // Yellow
        private static readonly Vector4 StatusBarColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f); // Gray
        private static readonly Vector4 BackgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f); // Black
        
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
                    _terminal.SetCell(x, y, ' ', NarrativeColor, BackgroundColor);
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
            _terminal.Text(titleX, 0, title, HeaderColor, BackgroundColor);
            
            // Line 1: Sublocation and turn info
            string sublocInfo = $"{sublocation}";
            string turnInfo = $"Turn {turnCount}";
            _terminal.Text(2, 1, sublocInfo, NarrativeColor, BackgroundColor);
            _terminal.Text(TERMINAL_WIDTH - turnInfo.Length - 2, 1, turnInfo, NarrativeColor, BackgroundColor);
            
            // Line 2: Time and weather (if provided)
            if (!string.IsNullOrEmpty(timeOfDay) || !string.IsNullOrEmpty(weather))
            {
                string envInfo = "";
                if (!string.IsNullOrEmpty(timeOfDay)) envInfo += timeOfDay;
                if (!string.IsNullOrEmpty(weather))
                {
                    if (envInfo.Length > 0) envInfo += " | ";
                    envInfo += weather;
                }
                _terminal.Text(2, 2, envInfo, NarrativeColor, BackgroundColor);
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
                
                _terminal.Text(2, y, line, NarrativeColor, BackgroundColor);
                y++;
            }
        }
        
        /// <summary>
        /// Renders the action menu with mouse-hover support
        /// </summary>
        public void RenderActionMenu(List<string> actions, int? hoveredIndex = null)
        {
            _actionRegions.Clear();
            
            if (actions == null || actions.Count == 0)
            {
                _terminal.Text(2, ACTION_MENU_START_Y, "No actions available.", StatusBarColor, BackgroundColor);
                return;
            }
            
            // Draw separator before actions
            DrawHorizontalLine(ACTION_MENU_START_Y - 1);
            
            int y = ACTION_MENU_START_Y;
            int maxY = TERMINAL_HEIGHT - STATUS_BAR_HEIGHT - 1;
            
            for (int i = 0; i < actions.Count && y < maxY; i++)
            {
                string action = actions[i];
                bool isHovered = hoveredIndex.HasValue && hoveredIndex.Value == i;
                Vector4 color = isHovered ? ActionHoverColor : ActionNormalColor;
                
                // Format: "1. Action text here"
                string prefix = $"{i + 1}. ";
                int prefixLen = prefix.Length;
                int maxActionWidth = TERMINAL_WIDTH - 4 - prefixLen; // Margins + prefix
                
                // Wrap action text if needed
                List<string> actionLines = WrapText(action, maxActionWidth);
                
                int startY = y;
                
                // Render first line with number prefix
                if (actionLines.Count > 0)
                {
                    _terminal.Text(2, y, prefix + actionLines[0], color, BackgroundColor);
                    y++;
                }
                
                // Render continuation lines with indentation
                for (int lineIdx = 1; lineIdx < actionLines.Count && y < maxY; lineIdx++)
                {
                    string indent = new string(' ', prefixLen);
                    _terminal.Text(2, y, indent + actionLines[lineIdx], color, BackgroundColor);
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
            
            _terminal.Text(2, statusY, message, StatusBarColor, BackgroundColor);
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
        public void UpdateHover(int mouseX, int mouseY, List<string> actions)
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
                _terminal.SetCell(x, y, '─', StatusBarColor, BackgroundColor);
            }
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
                    _terminal.SetCell(x, y, ' ', NarrativeColor, BackgroundColor);
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
                    _terminal.Text(x, y, line, color, BackgroundColor);
                }
            }
        }
        
        /// <summary>
        /// Renders a complete location UI frame with all sections
        /// </summary>
        public void RenderComplete(
            string locationName,
            string sublocation,
            int turnCount,
            string narrative,
            List<string> actions,
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
