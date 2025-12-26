using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// Renders Phase 6 Chain-of-Thought observation UI with scrollable narration,
/// highlighted keywords, and hover interactions.
/// </summary>
public class Phase6ObservationUI
{
    // Terminal dimensions
    private const int TERMINAL_WIDTH = 100;
    private const int TERMINAL_HEIGHT = 30;
    
    // Layout constants
    private const int HEADER_HEIGHT = 2;
    private const int STATUS_BAR_HEIGHT = 1;
    private const int NARRATIVE_START_Y = HEADER_HEIGHT;
    private const int NARRATIVE_HEIGHT = TERMINAL_HEIGHT - HEADER_HEIGHT - STATUS_BAR_HEIGHT;
    private const int SCROLLBAR_X = TERMINAL_WIDTH - 1; // Right edge
    private const int SCROLLBAR_TRACK_START_Y = NARRATIVE_START_Y;
    private const int SCROLLBAR_TRACK_HEIGHT = NARRATIVE_HEIGHT;
    
    // Colors
    private static readonly Vector4 HeaderColor = new(0.0f, 0.8f, 1.0f, 1.0f); // Cyan
    private static readonly Vector4 SkillHeaderColor = new(1.0f, 1.0f, 0.0f, 1.0f); // Yellow
    private static readonly Vector4 NarrativeColor = new(0.7f, 0.7f, 0.7f, 1.0f); // Gray70
    private static readonly Vector4 KeywordNormalColor = new(0.5f, 0.9f, 1.0f, 1.0f); // Light cyan
    private static readonly Vector4 KeywordHoverColor = new(1.0f, 1.0f, 1.0f, 1.0f); // White
    private static readonly Vector4 ActionNormalColor = new(1.0f, 1.0f, 1.0f, 1.0f); // White
    private static readonly Vector4 ActionHoverColor = new(1.0f, 1.0f, 0.0f, 1.0f); // Yellow
    private static readonly Vector4 ActionSkillColor = new(0.5f, 1.0f, 0.5f, 1.0f); // Light green
    private static readonly Vector4 ReasoningColor = new(0.8f, 0.8f, 0.9f, 1.0f); // Light purple-gray
    private static readonly Vector4 ScrollbarTrackColor = new(0.3f, 0.3f, 0.3f, 1.0f); // Dark gray
    private static readonly Vector4 ScrollbarThumbColor = new(0.6f, 0.6f, 0.6f, 1.0f); // Medium gray
    private static readonly Vector4 ScrollbarThumbHoverColor = new(0.8f, 0.8f, 0.8f, 1.0f); // Light gray
    private static readonly Vector4 StatusBarColor = new(0.5f, 0.5f, 0.5f, 1.0f); // Gray
    private static readonly Vector4 BackgroundColor = new(0.0f, 0.0f, 0.0f, 1.0f); // Black
    private static readonly Vector4 ErrorColor = new(1.0f, 0.3f, 0.3f, 1.0f); // Red
    private static readonly Vector4 LoadingColor = new(0.8f, 0.8f, 0.0f, 1.0f); // Yellow
    
    // Loading animation
    private static readonly string[] LoadingFrames = new[]
    {
        "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"
    };
    private int _loadingFrameIndex = 0;
    private DateTime _lastFrameUpdate = DateTime.Now;
    
    private readonly TerminalHUD _terminal;
    private List<KeywordRegion> _keywordRegions = new();
    private List<ActionRegion> _actionRegions = new();
    
    public Phase6ObservationUI(TerminalHUD terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        
        if (_terminal.Width != TERMINAL_WIDTH || _terminal.Height != TERMINAL_HEIGHT)
        {
            throw new ArgumentException($"Terminal must be {TERMINAL_WIDTH}x{TERMINAL_HEIGHT}, but got {_terminal.Width}x{_terminal.Height}");
        }
    }
    
    /// <summary>
    /// Clear the entire terminal.
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
        _keywordRegions.Clear();
    }
    
    /// <summary>
    /// Render the header with location name and thinking attempts.
    /// </summary>
    public void RenderHeader(string locationName, int thinkingAttemptsRemaining)
    {
        // Line 0: Location name
        string title = $"Forest Exploration - {locationName}";
        _terminal.Text(2, 0, title, HeaderColor, BackgroundColor);
        
        // Thinking attempts indicator (right side)
        string attempts = $"Thinking: ";
        int attemptsX = TERMINAL_WIDTH - 20;
        _terminal.Text(attemptsX, 0, attempts, StatusBarColor, BackgroundColor);
        
        // Draw filled boxes for remaining attempts
        int boxX = attemptsX + attempts.Length;
        for (int i = 0; i < 3; i++)
        {
            string box = i < thinkingAttemptsRemaining ? "[██]" : "[  ]";
            Vector4 boxColor = i < thinkingAttemptsRemaining 
                ? new Vector4(0.8f, 0.4f, 0.4f, 1.0f) // Red-ish for available
                : new Vector4(0.3f, 0.3f, 0.3f, 1.0f); // Dark gray for used
            _terminal.Text(boxX, 0, box, boxColor, BackgroundColor);
            boxX += 5;
        }
        
        // Separator line
        DrawHorizontalLine(1);
    }
    
    /// <summary>
    /// Render observation blocks with keywords highlighted.
    /// </summary>
    public void RenderObservationBlocks(
        NarrationScrollBuffer scrollBuffer,
        int scrollOffset,
        KeywordRegion? hoveredKeyword = null,
        ActionRegion? hoveredAction = null)
    {
        _keywordRegions.Clear();
        _actionRegions.Clear();
        
        // Clear narrative area
        for (int y = NARRATIVE_START_Y; y < TERMINAL_HEIGHT - STATUS_BAR_HEIGHT; y++)
        {
            for (int x = 0; x < TERMINAL_WIDTH; x++)
            {
                _terminal.SetCell(x, y, ' ', NarrativeColor, BackgroundColor);
            }
        }
        
        // Get visible lines based on scroll offset
        var visibleLines = scrollBuffer.GetVisibleLines(scrollOffset, NARRATIVE_HEIGHT);
        
        int currentY = NARRATIVE_START_Y;
        foreach (var renderedLine in visibleLines)
        {
            if (currentY >= TERMINAL_HEIGHT - STATUS_BAR_HEIGHT)
                break;
            
            switch (renderedLine.Type)
            {
                case LineType.Header:
                    // Render skill name header in yellow
                    _terminal.Text(2, currentY, renderedLine.Text, SkillHeaderColor, BackgroundColor);
                    break;
                    
                case LineType.Content:
                    // Render content with keyword highlighting
                    RenderLineWithKeywords(
                        renderedLine.Text,
                        renderedLine.Keywords,
                        2,
                        currentY,
                        hoveredKeyword);
                    break;
                    
                case LineType.Action:
                    // Render actions list
                    if (renderedLine.Actions != null && renderedLine.Actions.Count > 0)
                    {
                        currentY = RenderActionsBlock(renderedLine.Actions, currentY, hoveredAction);
                        continue; // RenderActionsBlock handles Y advancement
                    }
                    break;
                    
                case LineType.Empty:
                    // Just skip (already cleared)
                    break;
            }
            
            currentY++;
        }
    }
    
    /// <summary>
    /// Render a single line of text with keywords highlighted.
    /// </summary>
    private void RenderLineWithKeywords(
        string text,
        List<string>? keywords,
        int startX,
        int y,
        KeywordRegion? hoveredKeyword)
    {
        if (string.IsNullOrEmpty(text))
            return;
        
        if (keywords == null || keywords.Count == 0)
        {
            // No keywords, just render normal text
            _terminal.Text(startX, y, text, NarrativeColor, BackgroundColor);
            return;
        }
        
        // Find all keyword occurrences in this line (case-insensitive)
        var keywordOccurrences = new List<(int start, int length, string keyword)>();
        
        foreach (var keyword in keywords)
        {
            int index = 0;
            while ((index = text.IndexOf(keyword, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                keywordOccurrences.Add((index, keyword.Length, keyword));
                index += keyword.Length;
            }
        }
        
        // Sort by position
        keywordOccurrences = keywordOccurrences.OrderBy(k => k.start).ToList();
        
        // Render text with keywords highlighted
        int currentPos = 0;
        int currentX = startX;
        
        foreach (var (start, length, keyword) in keywordOccurrences)
        {
            // Render text before keyword
            if (start > currentPos)
            {
                string beforeText = text.Substring(currentPos, start - currentPos);
                _terminal.Text(currentX, y, beforeText, NarrativeColor, BackgroundColor);
                currentX += beforeText.Length;
            }
            
            // Render keyword with highlighting
            string keywordText = text.Substring(start, length);
            
            // Track keyword region for click detection
            var keywordRegion = new KeywordRegion(keyword, y, currentX, currentX + keywordText.Length - 1);
            _keywordRegions.Add(keywordRegion);
            
            // Check if this specific region is hovered (not just any instance of the keyword)
            bool isHovered = hoveredKeyword != null &&
                           hoveredKeyword.Y == y &&
                           hoveredKeyword.StartX == currentX &&
                           hoveredKeyword.EndX == currentX + keywordText.Length - 1;
            Vector4 keywordColor = isHovered ? KeywordHoverColor : KeywordNormalColor;
            _terminal.Text(currentX, y, keywordText, keywordColor, BackgroundColor);
            
            currentX += keywordText.Length;
            currentPos = start + length;
        }
        
        // Render remaining text
        if (currentPos < text.Length)
        {
            string remainingText = text.Substring(currentPos);
            _terminal.Text(currentX, y, remainingText, NarrativeColor, BackgroundColor);
        }
    }
    
    /// <summary>
    /// Get the keyword region under the mouse cursor, or null if none.
    /// </summary>
    public KeywordRegion? GetHoveredKeyword(int mouseX, int mouseY)
    {
        foreach (var region in _keywordRegions)
        {
            if (mouseY == region.Y && mouseX >= region.StartX && mouseX <= region.EndX)
            {
                return region;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Render a list of actions with hover highlighting.
    /// Returns the new Y position after rendering all actions.
    /// </summary>
    private int RenderActionsBlock(List<ParsedNarrativeAction> actions, int startY, ActionRegion? hoveredAction)
    {
        int currentY = startY;
        int maxY = TERMINAL_HEIGHT - STATUS_BAR_HEIGHT;
        
        for (int i = 0; i < actions.Count && currentY < maxY; i++)
        {
            var action = actions[i];
            
            // Check if this specific action region is hovered
            var actionRegion = new ActionRegion(i, currentY, currentY, 2, TERMINAL_WIDTH - 2);
            bool isHovered = hoveredAction != null &&
                           hoveredAction.StartY == currentY &&
                           hoveredAction.ActionIndex == i;
            
            // Format: "> [SkillName] action text"
            string prefix = "> ";
            string skillBracket = $"[{action.ActionSkill?.DisplayName ?? action.ActionSkillId}] ";
            string actionText = action.DisplayText;  // Without "try to" prefix
            
            // Calculate colors
            Vector4 prefixColor = NarrativeColor;
            Vector4 skillColor = ActionSkillColor;
            Vector4 textColor = isHovered ? ActionHoverColor : ActionNormalColor;
            
            // Render the action line
            int startX = 2;
            _terminal.Text(startX, currentY, prefix, prefixColor, BackgroundColor);
            startX += prefix.Length;
            
            _terminal.Text(startX, currentY, skillBracket, skillColor, BackgroundColor);
            startX += skillBracket.Length;
            
            int actionTextStartX = startX;
            _terminal.Text(startX, currentY, actionText, textColor, BackgroundColor);
            
            // Track action region for click detection
            _actionRegions.Add(actionRegion);
            
            currentY++;
        }
        
        return currentY;
    }
    
    /// <summary>
    /// Get the action region under the mouse cursor, or null if none.
    /// </summary>
    public ActionRegion? GetHoveredAction(int mouseX, int mouseY)
    {
        foreach (var region in _actionRegions)
        {
            if (mouseY >= region.StartY && mouseY <= region.EndY &&
                mouseX >= region.StartX && mouseX <= region.EndX)
            {
                return region;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Render the scrollbar on the right edge.
    /// Returns the thumb position (StartY, Height) for hit detection.
    /// </summary>
    public (int StartY, int Height) RenderScrollbar(
        NarrationScrollBuffer scrollBuffer,
        int scrollOffset,
        bool isThumbHovered)
    {
        int trackStartY = SCROLLBAR_TRACK_START_Y;
        int trackHeight = SCROLLBAR_TRACK_HEIGHT;
        int scrollbarX = SCROLLBAR_X;
        
        // Draw track
        for (int y = trackStartY; y < trackStartY + trackHeight; y++)
        {
            _terminal.SetCell(scrollbarX, y, '│', ScrollbarTrackColor, BackgroundColor);
        }
        
        // Calculate thumb size and position
        int totalLines = scrollBuffer.TotalLines;
        int visibleLines = NARRATIVE_HEIGHT;
        
        // If content fits in viewport, no thumb needed
        if (totalLines <= visibleLines)
        {
            return (0, 0);
        }
        
        // Calculate thumb size (proportional to visible area)
        float visibleRatio = (float)visibleLines / totalLines;
        int thumbHeight = Math.Max(2, (int)(trackHeight * visibleRatio));
        
        // Calculate thumb position based on scroll offset
        int maxScrollOffset = totalLines - visibleLines;
        float scrollRatio = maxScrollOffset > 0 ? (float)scrollOffset / maxScrollOffset : 0f;
        int maxThumbY = trackHeight - thumbHeight;
        int thumbY = trackStartY + (int)(maxThumbY * scrollRatio);
        
        // Render thumb
        Vector4 thumbColor = isThumbHovered ? ScrollbarThumbHoverColor : ScrollbarThumbColor;
        for (int y = thumbY; y < thumbY + thumbHeight; y++)
        {
            _terminal.SetCell(scrollbarX, y, '█', thumbColor, BackgroundColor);
        }
        
        return (thumbY, thumbHeight);
    }
    
    /// <summary>
    /// Check if mouse is over scrollbar thumb.
    /// </summary>
    public bool IsMouseOverScrollbarThumb(int mouseX, int mouseY, (int StartY, int Height) thumb)
    {
        if (thumb.Height == 0) return false; // No thumb rendered
        return mouseX == SCROLLBAR_X &&
               mouseY >= thumb.StartY &&
               mouseY < thumb.StartY + thumb.Height;
    }
    
    /// <summary>
    /// Check if mouse is over scrollbar track (but not thumb).
    /// </summary>
    public bool IsMouseOverScrollbarTrack(int mouseX, int mouseY, (int StartY, int Height) thumb)
    {
        if (mouseX != SCROLLBAR_X) return false;
        int trackStartY = SCROLLBAR_TRACK_START_Y;
        int trackEndY = trackStartY + SCROLLBAR_TRACK_HEIGHT;
        
        bool inTrack = mouseY >= trackStartY && mouseY < trackEndY;
        bool onThumb = thumb.Height > 0 && mouseY >= thumb.StartY && mouseY < thumb.StartY + thumb.Height;
        
        return inTrack && !onThumb;
    }
    
    /// <summary>
    /// Calculate scroll offset from mouse Y position on scrollbar.
    /// </summary>
    public int CalculateScrollOffsetFromMouseY(int mouseY, NarrationScrollBuffer scrollBuffer)
    {
        int trackStartY = SCROLLBAR_TRACK_START_Y;
        int trackHeight = SCROLLBAR_TRACK_HEIGHT;
        int totalLines = scrollBuffer.TotalLines;
        int visibleLines = NARRATIVE_HEIGHT;
        
        // Clamp mouse Y to track bounds
        int relativeY = Math.Clamp(mouseY - trackStartY, 0, trackHeight - 1);
        
        // Calculate scroll offset
        int maxScrollOffset = Math.Max(0, totalLines - visibleLines);
        float scrollRatio = (float)relativeY / (trackHeight - 1);
        int newOffset = (int)(maxScrollOffset * scrollRatio);
        
        return Math.Clamp(newOffset, 0, maxScrollOffset);
    }
    
    /// <summary>
    /// Render the status bar at the bottom.
    /// </summary>
    public void RenderStatusBar(string message = "")
    {
        int statusY = TERMINAL_HEIGHT - 1;
        
        if (string.IsNullOrEmpty(message))
        {
            message = "Hover keywords to highlight • Click keywords to think (3 attempts remaining)";
        }
        
        // Truncate if too long
        if (message.Length > TERMINAL_WIDTH - 4)
        {
            message = message.Substring(0, TERMINAL_WIDTH - 7) + "...";
        }
        
        _terminal.Text(2, statusY, message, StatusBarColor, BackgroundColor);
    }
    
    /// <summary>
    /// Show animated loading indicator.
    /// </summary>
    public void ShowLoadingIndicator(string message = "Generating observations...")
    {
        // Update animation frame every 100ms
        if ((DateTime.Now - _lastFrameUpdate).TotalMilliseconds > 100)
        {
            _loadingFrameIndex = (_loadingFrameIndex + 1) % LoadingFrames.Length;
            _lastFrameUpdate = DateTime.Now;
        }
        
        string spinner = LoadingFrames[_loadingFrameIndex];
        
        // Clear narrative area
        for (int y = NARRATIVE_START_Y; y < TERMINAL_HEIGHT - STATUS_BAR_HEIGHT; y++)
        {
            for (int x = 0; x < TERMINAL_WIDTH; x++)
            {
                _terminal.SetCell(x, y, ' ', NarrativeColor, BackgroundColor);
            }
        }
        
        // Show spinner and message centered
        string loadingText = $"{spinner}  {message}  {spinner}";
        int centerY = NARRATIVE_START_Y + NARRATIVE_HEIGHT / 2;
        int centerX = (TERMINAL_WIDTH - loadingText.Length) / 2;
        _terminal.Text(centerX, centerY, loadingText, LoadingColor, BackgroundColor);
        
        // Add animated dots
        string dots = new string('.', (_loadingFrameIndex % 4));
        string hint = $"Please wait{dots}";
        int hintY = centerY + 2;
        int hintX = (TERMINAL_WIDTH - hint.Length) / 2;
        _terminal.Text(hintX, hintY, hint, StatusBarColor, BackgroundColor);
        
        // Progress bar
        int barWidth = 30;
        int barY = centerY - 2;
        int barX = (TERMINAL_WIDTH - barWidth) / 2;
        string progressBar = GenerateProgressBar(barWidth, _loadingFrameIndex);
        _terminal.Text(barX, barY, progressBar, LoadingColor, BackgroundColor);
    }
    
    /// <summary>
    /// Show error message.
    /// </summary>
    public void ShowError(string errorMessage)
    {
        // Clear narrative area
        for (int y = NARRATIVE_START_Y; y < TERMINAL_HEIGHT - STATUS_BAR_HEIGHT; y++)
        {
            for (int x = 0; x < TERMINAL_WIDTH; x++)
            {
                _terminal.SetCell(x, y, ' ', NarrativeColor, BackgroundColor);
            }
        }
        
        // Show error message centered
        string title = "ERROR";
        int titleY = NARRATIVE_START_Y + NARRATIVE_HEIGHT / 2 - 2;
        int titleX = (TERMINAL_WIDTH - title.Length) / 2;
        _terminal.Text(titleX, titleY, title, ErrorColor, BackgroundColor);
        
        // Wrap error message
        var wrappedLines = WrapText(errorMessage, TERMINAL_WIDTH - 8);
        int startY = titleY + 2;
        
        for (int i = 0; i < wrappedLines.Count && startY + i < TERMINAL_HEIGHT - STATUS_BAR_HEIGHT; i++)
        {
            string line = wrappedLines[i];
            int x = (TERMINAL_WIDTH - line.Length) / 2;
            _terminal.Text(x, startY + i, line, ErrorColor, BackgroundColor);
        }
        
        // Show instruction
        string instruction = "(Press ESC to return)";
        int instructionY = TERMINAL_HEIGHT - STATUS_BAR_HEIGHT - 2;
        int instructionX = (TERMINAL_WIDTH - instruction.Length) / 2;
        _terminal.Text(instructionX, instructionY, instruction, StatusBarColor, BackgroundColor);
    }
    
    /// <summary>
    /// Draw horizontal separator line.
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
    /// Generate animated progress bar.
    /// </summary>
    private string GenerateProgressBar(int width, int frame)
    {
        var bar = new System.Text.StringBuilder();
        bar.Append('[');
        
        for (int i = 0; i < width - 2; i++)
        {
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
    /// Wrap text to fit within maximum width.
    /// </summary>
    private List<string> WrapText(string text, int maxWidth)
    {
        var lines = new List<string>();
        
        if (string.IsNullOrEmpty(text))
            return lines;
        
        var paragraphs = text.Split(new[] { '\n', '\r' }, StringSplitOptions.None);
        
        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                lines.Add("");
                continue;
            }
            
            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var currentLine = new System.Text.StringBuilder();
            
            foreach (var word in words)
            {
                var testLine = currentLine.Length == 0 ? word : currentLine + " " + word;
                
                if (testLine.Length <= maxWidth)
                {
                    if (currentLine.Length > 0)
                        currentLine.Append(' ');
                    currentLine.Append(word);
                }
                else
                {
                    if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                    }
                    
                    if (word.Length > maxWidth)
                    {
                        lines.Add(word.Substring(0, maxWidth));
                        currentLine.Append(word.Substring(maxWidth));
                    }
                    else
                    {
                        currentLine.Append(word);
                    }
                }
            }
            
            if (currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString());
            }
        }
        
        return lines;
    }
}
