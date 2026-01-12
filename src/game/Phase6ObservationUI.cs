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
    // Use centralized layout constants
    private const int SCROLLBAR_X = Phase6Layout.TERMINAL_WIDTH - Phase6Layout.RIGHT_MARGIN; // Inside right margin
    
    // Loading animation
    private static readonly string[] LoadingFrames = new[]
    {
        "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"
    };
    private int _loadingFrameIndex = 0;
    private DateTime _lastFrameUpdate = DateTime.Now;
    
    private readonly TerminalHUD _terminal;
    private readonly KeywordRenderer _keywordRenderer;
    private List<KeywordRegion> _keywordRegions = new();
    private List<ActionRegion> _actionRegions = new();
    
    public Phase6ObservationUI(TerminalHUD terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _keywordRenderer = new KeywordRenderer();
        
        if (_terminal.Width != Phase6Layout.TERMINAL_WIDTH || _terminal.Height != Phase6Layout.TERMINAL_HEIGHT)
        {
            throw new ArgumentException($"Terminal must be {Phase6Layout.TERMINAL_WIDTH}x{Phase6Layout.TERMINAL_HEIGHT}, but got {_terminal.Width}x{_terminal.Height}");
        }
    }
    
    /// <summary>
    /// Clear the entire terminal.
    /// </summary>
    public void Clear()
    {
        for (int y = 0; y < Phase6Layout.TERMINAL_HEIGHT; y++)
        {
            for (int x = 0; x < Phase6Layout.TERMINAL_WIDTH; x++)
            {
                _terminal.SetCell(x, y, ' ', Config.Phase6UI.NarrativeColor, Config.Phase6UI.BackgroundColor);
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
        _terminal.Text(Phase6Layout.LEFT_MARGIN, 0, title, Config.Phase6UI.HeaderColor, Config.Phase6UI.BackgroundColor);
        
        // Thinking attempts indicator (right side)
        string attempts = $"Thinking: ";
        int attemptsX = Phase6Layout.TERMINAL_WIDTH - Phase6Layout.RIGHT_MARGIN - 20;
        _terminal.Text(attemptsX, 0, attempts, Config.Phase6UI.StatusBarColor, Config.Phase6UI.BackgroundColor);
        
        // Draw filled boxes for remaining attempts
        int boxX = attemptsX + attempts.Length;
        for (int i = 0; i < 3; i++)
        {
            string box = i < thinkingAttemptsRemaining ? "[██]" : "[  ]";
            Vector4 boxColor = i < thinkingAttemptsRemaining 
                ? new Vector4(0.8f, 0.4f, 0.4f, 1.0f) // Red-ish for available
                : new Vector4(0.3f, 0.3f, 0.3f, 1.0f); // Dark gray for used
            _terminal.Text(boxX, 0, box, boxColor, Config.Phase6UI.BackgroundColor);
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
        for (int y = Phase6Layout.CONTENT_START_Y; y < Phase6Layout.SEPARATOR_Y + 1; y++)
        {
            for (int x = 0; x < Phase6Layout.TERMINAL_WIDTH; x++)
            {
                _terminal.SetCell(x, y, ' ', Config.Phase6UI.NarrativeColor, Config.Phase6UI.BackgroundColor);
            }
        }
        
        // Get visible lines based on scroll offset
        // Subtract 1 from NARRATIVE_HEIGHT to account for the bottom separator line
        int visibleContentHeight = Phase6Layout.NARRATIVE_HEIGHT - Phase6Layout.SEPARATOR_HEIGHT;
        var visibleLines = scrollBuffer.GetVisibleLines(scrollOffset, visibleContentHeight);
        
        int currentY = Phase6Layout.CONTENT_START_Y;
        int currentActionIndex = -1;
        ParsedNarrativeAction? currentAction = null;
        int actionLineCount = 0;
        
        foreach (var renderedLine in visibleLines)
        {
            if (currentY >= Phase6Layout.CONTENT_END_Y + 1)
                break;
            
            switch (renderedLine.Type)
            {
                case LineType.Header:
                    // Render skill name header in yellow
                    _terminal.Text(Phase6Layout.LEFT_MARGIN, currentY, renderedLine.Text, Config.Phase6UI.SkillHeaderColor, Config.Phase6UI.BackgroundColor);
                    
                    // Note: Do NOT reset action counter here - we want globally unique action indices
                    // so that actions from different thinking blocks don't have the same index
                    break;
                    
                case LineType.Content:
                    // Render content with keyword highlighting
                    RenderLineWithKeywords(
                        renderedLine.Text,
                        renderedLine.Keywords,
                        Phase6Layout.LEFT_MARGIN,
                        currentY,
                        hoveredKeyword);
                    break;
                    
                case LineType.Action:
                    // Check if this is an action line from Thinking block OR an action result
                    if (renderedLine.Actions != null && renderedLine.Actions.Count > 0)
                    {
                        // This is a thinking block action (clickable)
                        currentAction = renderedLine.Actions[0];
                        currentActionIndex++;
                        actionLineCount = 0;
                        RenderActionLine(renderedLine.Text, currentAction, currentActionIndex, currentY, actionLineCount, hoveredAction);
                        actionLineCount++;
                    }
                    else if (currentAction != null)
                    {
                        // Continuation line of current action
                        RenderActionLine(renderedLine.Text, currentAction, currentActionIndex, currentY, actionLineCount, hoveredAction);
                        actionLineCount++;
                    }
                    else
                    {
                        // Action result block (from Action block type) - detect SUCCESS/FAILURE
                        Vector4 actionColor = Config.Phase6UI.NarrativeColor;
                        if (renderedLine.Text.Contains("[SUCCESS]"))
                        {
                            actionColor = Config.Phase6UI.SuccessColor;
                        }
                        else if (renderedLine.Text.Contains("[FAILURE]"))
                        {
                            actionColor = Config.Phase6UI.FailureColor;
                        }
                        _terminal.Text(Phase6Layout.LEFT_MARGIN, currentY, renderedLine.Text, actionColor, Config.Phase6UI.BackgroundColor);
                    }
                    break;
                    
                case LineType.Outcome:
                    // Outcome narration - check if previous line in buffer contains SUCCESS/FAILURE
                    Vector4 outcomeColor = Config.Phase6UI.NarrativeColor;
                    
                    // Look back in the visible lines to find the action result
                    int lookbackIndex = visibleLines.IndexOf(renderedLine) - 1;
                    while (lookbackIndex >= 0)
                    {
                        var prevLine = visibleLines[lookbackIndex];
                        if (prevLine.Type == LineType.Action && prevLine.BlockType == NarrationBlockType.Action)
                        {
                            // Found the action result line
                            if (prevLine.Text.Contains("[SUCCESS]"))
                            {
                                outcomeColor = Config.Phase6UI.SuccessColor;
                            }
                            else if (prevLine.Text.Contains("[FAILURE]"))
                            {
                                outcomeColor = Config.Phase6UI.FailureColor;
                            }
                            break;
                        }
                        lookbackIndex--;
                    }
                    
                    _terminal.Text(Phase6Layout.LEFT_MARGIN, currentY, renderedLine.Text, outcomeColor, Config.Phase6UI.BackgroundColor);
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
            _terminal.Text(startX, y, text, Config.Phase6UI.NarrativeColor, Config.Phase6UI.BackgroundColor);
            return;
        }
        
        // Use KeywordRenderer to properly parse keywords with morphological variations and word boundaries
        var segments = _keywordRenderer.ParseNarrationWithKeywords(text, keywords);
        
        int currentX = startX;
        
        foreach (var segment in segments)
        {
            if (segment.IsKeyword)
            {
                // Track keyword region for click detection
                var keywordRegion = new KeywordRegion(
                    segment.KeywordValue!, 
                    y, 
                    currentX, 
                    currentX + segment.Text.Length - 1);
                _keywordRegions.Add(keywordRegion);
                
                // Check if this specific region is hovered
                bool isHovered = hoveredKeyword != null &&
                               hoveredKeyword.Y == y &&
                               hoveredKeyword.StartX == currentX &&
                               hoveredKeyword.EndX == currentX + segment.Text.Length - 1;
                Vector4 keywordColor = isHovered ? Config.Phase6UI.KeywordHoverColor : Config.Phase6UI.KeywordNormalColor;
                _terminal.Text(currentX, y, segment.Text, keywordColor, Config.Phase6UI.BackgroundColor);
            }
            else
            {
                // Render normal text
                _terminal.Text(currentX, y, segment.Text, Config.Phase6UI.NarrativeColor, Config.Phase6UI.BackgroundColor);
            }
            
            currentX += segment.Text.Length;
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
    /// Render a single action line (actions are pre-wrapped in scroll buffer).
    /// </summary>
    private void RenderActionLine(string text, ParsedNarrativeAction action, int actionIndex, int y, int lineIndex, ActionRegion? hoveredAction)
    {
        // Check if this action is hovered
        bool isHovered = hoveredAction != null && hoveredAction.ActionIndex == actionIndex;
        
        // Calculate colors
        Vector4 prefixColor = Config.Phase6UI.NarrativeColor;
        Vector4 skillColor = Config.Phase6UI.ActionSkillColor;
        Vector4 textColor = isHovered ? Config.Phase6UI.ActionHoverColor : Config.Phase6UI.ActionNormalColor;
        
        int startX = Phase6Layout.LEFT_MARGIN;
        
        if (lineIndex == 0)
        {
            // First line: render with prefix and skill
            string prefix = "> ";
            string skillBracket = $"[{action.ActionSkill?.DisplayName ?? action.ActionSkillId}] ";
            
            _terminal.Text(startX, y, prefix, prefixColor, Config.Phase6UI.BackgroundColor);
            startX += prefix.Length;
            
            _terminal.Text(startX, y, skillBracket, skillColor, Config.Phase6UI.BackgroundColor);
            startX += skillBracket.Length;
            
            _terminal.Text(startX, y, text, textColor, Config.Phase6UI.BackgroundColor);
            
            // Track action region (will be updated as we encounter more lines)
            var actionRegion = new ActionRegion(actionIndex, y, y, Phase6Layout.LEFT_MARGIN, Phase6Layout.TERMINAL_WIDTH - Phase6Layout.RIGHT_MARGIN);
            _actionRegions.Add(actionRegion);
        }
        else
        {
            // Continuation line: indent by 4 spaces
            int continuationIndent = Phase6Layout.LEFT_MARGIN + 4;
            _terminal.Text(continuationIndent, y, text, textColor, Config.Phase6UI.BackgroundColor);
            
            // Update the action region to extend to this line
            if (_actionRegions.Count > 0)
            {
                var lastRegion = _actionRegions[_actionRegions.Count - 1];
                if (lastRegion.ActionIndex == actionIndex)
                {
                    _actionRegions[_actionRegions.Count - 1] = new ActionRegion(
                        actionIndex, 
                        lastRegion.StartY, 
                        y,  // Extend to current line
                        Phase6Layout.LEFT_MARGIN,
                        Phase6Layout.TERMINAL_WIDTH - Phase6Layout.RIGHT_MARGIN
                    );
                }
            }
        }
    }
    
    /// <summary>
    // Note: RenderActionsBlock() and WrapActionText() removed - actions are now pre-wrapped 
    // in NarrationScrollBuffer and rendered via RenderActionLine() for each wrapped line.
    
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
        int trackStartY = Phase6Layout.CONTENT_START_Y;
        int trackHeight = Phase6Layout.SCROLLBAR_TRACK_HEIGHT;
        int scrollbarX = SCROLLBAR_X;
        
        // Draw track
        for (int y = trackStartY; y < trackStartY + trackHeight; y++)
        {
            _terminal.SetCell(scrollbarX, y, '│', Config.Phase6UI.ScrollbarTrackColor, Config.Phase6UI.BackgroundColor);
        }
        
        // Calculate thumb size and position
        int totalLines = scrollBuffer.TotalLines;
        int visibleLines = Phase6Layout.NARRATIVE_HEIGHT;
        
        // If content fits in viewport, no thumb needed
        if (totalLines <= visibleLines)
        {
            return (0, 0);
        }
        
        // Calculate thumb size (proportional to visible area)
        float visibleRatio = (float)visibleLines / totalLines;
        int thumbHeight = Math.Max(2, (int)(trackHeight * visibleRatio));
        
        // Calculate thumb position based on scroll offset
        int maxScrollOffset = Phase6Layout.CalculateMaxScrollOffset(totalLines);
        float scrollRatio = maxScrollOffset > 0 ? (float)scrollOffset / maxScrollOffset : 0f;
        int maxThumbY = trackHeight - thumbHeight;
        int thumbY = trackStartY + (int)(maxThumbY * scrollRatio);
        
        // Render thumb
        Vector4 thumbColor = isThumbHovered ? Config.Phase6UI.ScrollbarThumbHoverColor : Config.Phase6UI.ScrollbarThumbColor;
        for (int y = thumbY; y < thumbY + thumbHeight; y++)
        {
            _terminal.SetCell(scrollbarX, y, '█', thumbColor, Config.Phase6UI.BackgroundColor);
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
        int trackStartY = Phase6Layout.CONTENT_START_Y;
        int trackEndY = trackStartY + Phase6Layout.SCROLLBAR_TRACK_HEIGHT;
        
        bool inTrack = mouseY >= trackStartY && mouseY < trackEndY;
        bool onThumb = thumb.Height > 0 && mouseY >= thumb.StartY && mouseY < thumb.StartY + thumb.Height;
        
        return inTrack && !onThumb;
    }
    
    /// <summary>
    /// Calculate scroll offset from mouse Y position on scrollbar.
    /// </summary>
    public int CalculateScrollOffsetFromMouseY(int mouseY, NarrationScrollBuffer scrollBuffer)
    {
        int trackStartY = Phase6Layout.CONTENT_START_Y;
        int trackHeight = Phase6Layout.SCROLLBAR_TRACK_HEIGHT;
        int totalLines = scrollBuffer.TotalLines;
        int visibleLines = Phase6Layout.NARRATIVE_HEIGHT - Phase6Layout.SEPARATOR_HEIGHT; // Account for separator line
        
        // Clamp mouse Y to track bounds
        int relativeY = Math.Clamp(mouseY - trackStartY, 0, trackHeight - 1);
        
        // Calculate scroll offset
        // Add 5-line margin to match NarrationScrollBuffer's maxScroll calculation
        int maxScrollOffset = Math.Max(0, totalLines - visibleLines + 5);
        float scrollRatio = (float)relativeY / (trackHeight - 1);
        int newOffset = (int)(maxScrollOffset * scrollRatio);
        
        return Math.Clamp(newOffset, 0, maxScrollOffset);
    }
    
    /// <summary>
    /// Render the status bar at the bottom.
    /// </summary>
    public void RenderStatusBar(string message = "")
    {
        int statusY = Phase6Layout.STATUS_BAR_Y;
        int separatorY = Phase6Layout.SEPARATOR_Y;
        
        // Draw separator line above status bar
        DrawHorizontalLine(separatorY);
        
        if (string.IsNullOrEmpty(message))
        {
            message = "Hover keywords to highlight • Click keywords to think (3 attempts remaining)";
        }
        
        // Truncate if too long
        int maxMessageWidth = Phase6Layout.CONTENT_WIDTH - 2;
        if (message.Length > maxMessageWidth)
        {
            message = message.Substring(0, maxMessageWidth - 3) + "...";
        }
        
        _terminal.Text(Phase6Layout.LEFT_MARGIN, statusY, message, Config.Phase6UI.StatusBarColor, Config.Phase6UI.BackgroundColor);
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
        for (int y = Phase6Layout.CONTENT_START_Y; y < Phase6Layout.SEPARATOR_Y + 1; y++)
        {
            for (int x = 0; x < Phase6Layout.TERMINAL_WIDTH; x++)
            {
                _terminal.SetCell(x, y, ' ', Config.Phase6UI.NarrativeColor, Config.Phase6UI.BackgroundColor);
            }
        }
        
        // Show spinner and message centered
        string loadingText = $"{spinner}  {message}  {spinner}";
        int centerY = Phase6Layout.CONTENT_START_Y + Phase6Layout.NARRATIVE_HEIGHT / 2;
        int centerX = (Phase6Layout.TERMINAL_WIDTH - loadingText.Length) / 2;
        _terminal.Text(centerX, centerY, loadingText, Config.Phase6UI.LoadingColor, Config.Phase6UI.BackgroundColor);
        
        // Add animated dots
        string dots = new string('.', (_loadingFrameIndex % 4));
        string hint = $"Please wait{dots}";
        int hintY = centerY + 2;
        int hintX = (Phase6Layout.TERMINAL_WIDTH - hint.Length) / 2;
        _terminal.Text(hintX, hintY, hint, Config.Phase6UI.StatusBarColor, Config.Phase6UI.BackgroundColor);
        
        // Progress bar
        int barWidth = 30;
        int barY = centerY - 2;
        int barX = (Phase6Layout.TERMINAL_WIDTH - barWidth) / 2;
        string progressBar = GenerateProgressBar(barWidth, _loadingFrameIndex);
        _terminal.Text(barX, barY, progressBar, Config.Phase6UI.LoadingColor, Config.Phase6UI.BackgroundColor);
    }
    
    /// <summary>
    /// Show error message.
    /// </summary>
    public void ShowError(string errorMessage)
    {
        // Clear narrative area
        for (int y = Phase6Layout.CONTENT_START_Y; y < Phase6Layout.SEPARATOR_Y + 1; y++)
        {
            for (int x = 0; x < Phase6Layout.TERMINAL_WIDTH; x++)
            {
                _terminal.SetCell(x, y, ' ', Config.Phase6UI.NarrativeColor, Config.Phase6UI.BackgroundColor);
            }
        }
        
        // Show error message centered
        string title = "ERROR";
        int titleY = Phase6Layout.CONTENT_START_Y + Phase6Layout.NARRATIVE_HEIGHT / 2 - 2;
        int titleX = (Phase6Layout.TERMINAL_WIDTH - title.Length) / 2;
        _terminal.Text(titleX, titleY, title, Config.Phase6UI.ErrorColor, Config.Phase6UI.BackgroundColor);
        
        // Wrap error message
        var wrappedLines = WrapText(errorMessage, Phase6Layout.CONTENT_WIDTH - 4);
        int startY = titleY + 2;
        
        for (int i = 0; i < wrappedLines.Count && startY + i < Phase6Layout.SEPARATOR_Y + 1; i++)
        {
            string line = wrappedLines[i];
            int x = (Phase6Layout.TERMINAL_WIDTH - line.Length) / 2;
            _terminal.Text(x, startY + i, line, Config.Phase6UI.ErrorColor, Config.Phase6UI.BackgroundColor);
        }
        
        // Show instruction
        string instruction = "(Press ESC to return)";
        int instructionY = Phase6Layout.SEPARATOR_Y - 1;
        int instructionX = (Phase6Layout.TERMINAL_WIDTH - instruction.Length) / 2;
        _terminal.Text(instructionX, instructionY, instruction, Config.Phase6UI.StatusBarColor, Config.Phase6UI.BackgroundColor);
    }
    
    /// <summary>
    /// Draw horizontal separator line.
    /// </summary>
    private void DrawHorizontalLine(int y)
    {
        if (y < 0 || y >= Phase6Layout.TERMINAL_HEIGHT)
            return;
        
        for (int x = 0; x < Phase6Layout.TERMINAL_WIDTH; x++)
        {
            _terminal.SetCell(x, y, '─', Config.Phase6UI.StatusBarColor, Config.Phase6UI.BackgroundColor);
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
    
    /// <summary>
    /// Render the "Continue" button at the bottom of the screen.
    /// Returns the button region for click detection.
    /// </summary>
    public (int X, int Y, int Width) RenderContinueButton(bool isHovered = false)
    {
        string buttonText = "[ Continue ]";
        int buttonWidth = buttonText.Length;
        int buttonX = (Phase6Layout.TERMINAL_WIDTH - buttonWidth) / 2;
        int buttonY = Phase6Layout.SEPARATOR_Y - 2; // Place near bottom, above separator
        
        Vector4 buttonColor = isHovered ? Config.Phase6UI.ContinueButtonHoverColor : Config.Phase6UI.ContinueButtonColor;
        
        _terminal.Text(buttonX, buttonY, buttonText, buttonColor, Config.Phase6UI.BackgroundColor);
        
        return (buttonX, buttonY, buttonWidth);
    }
    
    /// <summary>
    /// Check if mouse is over the continue button.
    /// </summary>
    public bool IsMouseOverContinueButton(int mouseX, int mouseY, (int X, int Y, int Width) buttonRegion)
    {
        return mouseY == buttonRegion.Y && 
               mouseX >= buttonRegion.X && 
               mouseX < buttonRegion.X + buttonRegion.Width;
    }
}
