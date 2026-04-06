using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// Renders Chain-of-Thought observation UI with scrollable narration,
/// highlighted keywords, and hover interactions.
/// </summary>
public class NarrativeUI : TerminalPanelUI
{
    // Dice roll animation — _loadingFrameIndex/_lastFrameUpdate live in TerminalPanelUI base
    
    private readonly KeywordRenderer _keywordRenderer;
    private int SCROLLBAR_X => _scrollbarX;  // alias to base protected field
    private List<KeywordRegion> _keywordRegions = new();
    private List<ActionRegion> _actionRegions = new();
    
    public NarrativeUI(TerminalHUD terminal) : base(terminal)
    {
        _keywordRenderer = new KeywordRenderer();
        Console.WriteLine($"NarrativeUI: Initialized with {_terminal.Width}x{_terminal.Height} terminal (padding: T={_layout.TOP_PADDING} B={_layout.BOTTOM_PADDING} L={_layout.LEFT_PADDING} R={_layout.RIGHT_PADDING})");
    }
    
    /// <summary>
    /// Get maximum thinking attempts.
    /// TODO: This should be retrieved from the protagonist instance once that characteristic is implemented.
    /// </summary>
    public static int GetMaxThinkingAttempts()
    {
        // Placeholder implementation - will be replaced with protagonist characteristic
        return 13;
    }
    
    /// <summary>Clear the entire terminal then reset keyword/action tracking regions.</summary>
    public override void Clear()
    {
        base.Clear();
        _keywordRegions.Clear();
    }
    
    /// <summary>
    /// Render the header with location name and thinking attempts.
    /// </summary>
    public void RenderHeader(string locationName, int thinkingAttemptsRemaining, WorldContext? worldContext = null, string? activePartyMemberName = null, TimePeriod? timePeriod = null)
    {
        // Header starts after top padding
        int headerY = _layout.TOP_PADDING;

        // Line 0: Location name with world context display name
        string contextTitle = worldContext?.DisplayName ?? "Forest";
        string formattedLocationName = locationName.Replace("_", " ");
        string memberSuffix = activePartyMemberName != null ? $"  [{activePartyMemberName.ToUpper()}]" : "";
        string timeSuffix = timePeriod.HasValue ? $"  | {timePeriod.Value}" : "";
        string title = $"{contextTitle} - {formattedLocationName}{memberSuffix}{timeSuffix}";
        _terminal.Text(_layout.CONTENT_START_X, headerY, title, Config.NarrativeUI.HeaderColor, Config.NarrativeUI.BackgroundColor);
        
        // Thinking attempts indicator (right side)
        int maxAttempts = GetMaxThinkingAttempts();
        string attempts = $"Remaining noetic points : [";
        int attemptsX = _layout.CONTENT_END_X - 40;
        _terminal.Text(attemptsX, headerY, attempts, Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);
        
        // Draw individual attempt markers
        int markerX = attemptsX + attempts.Length;
        for (int i = 0; i < maxAttempts; i++)
        {
            bool isRemaining = i < thinkingAttemptsRemaining;
            Vector4 markerColor = isRemaining
                ? Config.NarrativeUI.LoadingColor // Bright yellow for available
                : Config.NarrativeUI.HistoryColor; // Dark gray for used
            _terminal.Text(markerX, headerY, Config.Symbols.NoeticPointMarker.ToString(), markerColor, Config.NarrativeUI.BackgroundColor);
            markerX++;
        }
        
        // Closing bracket
        _terminal.Text(markerX, headerY, "]", Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);
        
        // Separator line (after header)
        DrawHorizontalLine(_layout.TOP_PADDING + 1);
    }
    
    /// <summary>
    /// Render observation blocks with keywords highlighted.
    /// History lines (from previous narration nodes) are rendered in dark gray.
    /// </summary>
    public void RenderObservationBlocks(
        NarrationScrollBuffer scrollBuffer,
        int scrollOffset,
        int thinkingAttemptsRemaining,
        KeywordRegion? hoveredKeyword = null,
        ActionRegion? hoveredAction = null,
        bool dimContent = false)
    {
        _keywordRegions.Clear();
        _actionRegions.Clear();
        
        // Clear narrative area (preserve padding zones)
        for (int y = _layout.CONTENT_START_Y; y < _layout.SEPARATOR_Y + 1; y++)
        {
            for (int x = _layout.LEFT_PADDING; x < _layout.TERMINAL_WIDTH - _layout.RIGHT_PADDING; x++)
            {
                _terminal.SetCell(x, y, ' ', Config.NarrativeUI.NarrativeColor, Config.NarrativeUI.BackgroundColor);
            }
        }
        
        // Get visible lines based on scroll offset
        // Subtract 1 from NARRATIVE_HEIGHT to account for the bottom separator line
        int visibleContentHeight = _layout.NARRATIVE_HEIGHT - _layout.SEPARATOR_HEIGHT;
        var visibleLines = scrollBuffer.GetVisibleLines(scrollOffset, visibleContentHeight);
        
        // When dimming content, find the last outcome block to keep it highlighted
        int lastOutcomeBlockStart = -1;
        if (dimContent)
        {
            // Find the start of the last outcome block (working backwards)
            for (int i = visibleLines.Count - 1; i >= 0; i--)
            {
                var line = visibleLines[i];
                if (!line.IsHistory && line.BlockType == NarrationBlockType.Outcome)
                {
                    // Found an outcome line, now find the start of this block
                    lastOutcomeBlockStart = i;
                    // Go backwards to find the beginning of this block
                    for (int j = i - 1; j >= 0; j--)
                    {
                        var prevLine = visibleLines[j];
                        if (prevLine.IsHistory || prevLine.BlockType != NarrationBlockType.Outcome || 
                            (prevLine.Type == LineType.Empty && j > 0 && visibleLines[j-1].BlockType != NarrationBlockType.Outcome))
                        {
                            lastOutcomeBlockStart = j + 1;
                            break;
                        }
                        lastOutcomeBlockStart = j;
                    }
                    break;
                }
            }
        }
        
        int currentY = _layout.CONTENT_START_Y;
        ParsedNarrativeAction? currentAction = null;
        int actionLineCount = 0;
        
        foreach (var renderedLine in visibleLines)
        {
            if (currentY >= _layout.CONTENT_END_Y + 1)
                break;
            
            // Check if this is a history line (from previous narration nodes)
            if (renderedLine.IsHistory)
            {
                RenderHistoryLine(renderedLine, currentY);
                currentY++;
                continue;
            }
            
            // Determine if this specific line should be dimmed
            int lineIndex = visibleLines.IndexOf(renderedLine);
            bool shouldDimThisLine = dimContent && (lastOutcomeBlockStart == -1 || lineIndex < lastOutcomeBlockStart);
            
            switch (renderedLine.Type)
            {
                case LineType.Header:
                    // Parse modusMentis header to separate name from level indicators
                    string headerText = renderedLine.Text;
                    
                    // Find the last space followed by black squares (level indicators) and optional closing bracket
                    int lastSpaceIndex = -1;
                    for (int i = headerText.Length - 1; i >= 0; i--)
                    {
                        if (headerText[i] == ' ')
                        {
                            // Check if everything after this space is level indicators followed by optional closing bracket
                            bool allLevelIndicators = true;
                            bool foundLevelIndicators = false;
                            
                            for (int j = i + 1; j < headerText.Length; j++)
                            {
                                if (headerText[j] == Config.Symbols.ModusMentisLevelIndicator)
                                {
                                    foundLevelIndicators = true;
                                }
                                else if (headerText[j] == ']' && j == headerText.Length - 1)
                                {
                                    // Closing bracket at the end is allowed
                                    continue;
                                }
                                else
                                {
                                    allLevelIndicators = false;
                                    break;
                                }
                            }
                            
                            if (allLevelIndicators && foundLevelIndicators)
                            {
                                lastSpaceIndex = i;
                                break;
                            }
                        }
                    }
                    
                    if (lastSpaceIndex > 0)
                    {
                        // Render modusMentis name and level indicators separately
                        string modusMentisName = headerText.Substring(0, lastSpaceIndex);
                        string remainingPart = headerText.Substring(lastSpaceIndex);
                        
                        // Separate level indicators from closing bracket
                        string levelIndicators = remainingPart;
                        string closingBracket = "";
                        
                        if (remainingPart.EndsWith(']'))
                        {
                            levelIndicators = remainingPart.Substring(0, remainingPart.Length - 1);
                            closingBracket = "]";
                        }
                        
                        Vector4 modusMentisHeaderColor = shouldDimThisLine ? Config.NarrativeUI.DimmedContentColor : Config.NarrativeUI.ModusMentisHeaderColor;
                        
                        // Determine modusMentis level indicator color based on whether this specific block is in the hovered action's chain
                        Vector4 modusMentisLevelColor;
                        if (shouldDimThisLine)
                        {
                            modusMentisLevelColor = Config.NarrativeUI.DimmedContentColor;
                        }
                        else if (hoveredAction?.Action != null && renderedLine.SourceBlock != null)
                        {
                            // Check if this specific block is in the hovered action's chain (not just matching modusMentis)
                            bool isInChain = hoveredAction.Action.IsElementInChain(renderedLine.SourceBlock);
                            modusMentisLevelColor = isInChain ? Config.NarrativeUI.LoadingColor : Config.NarrativeUI.ModusMentisHeaderColor;
                        }
                        else
                        {
                            modusMentisLevelColor = Config.NarrativeUI.LoadingColor;
                        }
                        
                        _terminal.Text(_layout.CONTENT_START_X, currentY, modusMentisName, modusMentisHeaderColor, Config.NarrativeUI.BackgroundColor);
                        _terminal.Text(_layout.CONTENT_START_X + modusMentisName.Length, currentY, levelIndicators, modusMentisLevelColor, Config.NarrativeUI.BackgroundColor);
                        
                        // Render closing bracket in dark yellow (same as modusMentis name)
                        if (!string.IsNullOrEmpty(closingBracket))
                        {
                            _terminal.Text(_layout.CONTENT_START_X + modusMentisName.Length + levelIndicators.Length, currentY, closingBracket, modusMentisHeaderColor, Config.NarrativeUI.BackgroundColor);
                        }
                    }
                    else
                    {
                        // Fallback: render entire header in modusMentis header color
                        Vector4 modusMentisHeaderColor = shouldDimThisLine ? Config.NarrativeUI.DimmedContentColor : Config.NarrativeUI.ModusMentisHeaderColor;
                        _terminal.Text(_layout.CONTENT_START_X, currentY, headerText, modusMentisHeaderColor, Config.NarrativeUI.BackgroundColor);
                    }
                    
                    // Note: Do NOT reset action counter here - we want globally unique action indices
                    // so that actions from different thinking blocks don't have the same index
                    break;
                    
                case LineType.Content:
                    // Render content with keyword highlighting
                    RenderLineWithKeywords(
                        renderedLine.Text,
                        renderedLine.Keywords,
                        _layout.CONTENT_START_X,
                        currentY,
                        thinkingAttemptsRemaining,
                        hoveredKeyword,
                        shouldDimThisLine,
                        renderedLine.SourceBlock,
                        renderedLine.KeywordOccurrenceIndices);
                    break;
                    
                case LineType.Action:
                    // Check if this is an action line from Thinking block OR an action result
                    if (renderedLine.Actions != null && renderedLine.Actions.Count > 0)
                    {
                        // This is a thinking block action (clickable)
                        currentAction = renderedLine.Actions[0];
                        actionLineCount = 0;
                        // Use GlobalActionIndex stored in renderedLine
                        RenderActionLine(renderedLine.Text, currentAction, renderedLine.GlobalActionIndex, currentY, actionLineCount, hoveredAction, shouldDimThisLine);
                        actionLineCount++;
                    }
                    else if (currentAction != null)
                    {
                        // Continuation line of current action - use same GlobalActionIndex
                        RenderActionLine(renderedLine.Text, currentAction, renderedLine.GlobalActionIndex, currentY, actionLineCount, hoveredAction, shouldDimThisLine);
                        actionLineCount++;
                    }
                    else
                    {
                        // Action result block (from Action block type) - detect SUCCESS/FAILURE
                        Vector4 actionColor = Config.NarrativeUI.NarrativeColor;
                        if (renderedLine.Text.Contains("[SUCCESS]"))
                        {
                            actionColor = Config.NarrativeUI.SuccessColor;
                        }
                        else if (renderedLine.Text.Contains("[FAILURE]"))
                        {
                            actionColor = Config.NarrativeUI.FailureColor;
                        }
                        
                        // Apply dimming if needed
                        if (shouldDimThisLine)
                        {
                            actionColor = Config.NarrativeUI.DimmedContentColor;
                        }
                        
                        _terminal.Text(_layout.CONTENT_START_X, currentY, renderedLine.Text, actionColor, Config.NarrativeUI.BackgroundColor);
                    }
                    break;
                    
                case LineType.Outcome:
                    // Outcome narration - check if previous line in buffer contains SUCCESS/FAILURE
                    Vector4 outcomeColor = Config.NarrativeUI.NarrativeColor;
                    
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
                                outcomeColor = Config.NarrativeUI.SuccessColor;
                            }
                            else if (prevLine.Text.Contains("[FAILURE]"))
                            {
                                outcomeColor = Config.NarrativeUI.FailureColor;
                            }
                            break;
                        }
                        lookbackIndex--;
                    }
                    
                    // Apply dimming if needed
                    if (shouldDimThisLine)
                    {
                        outcomeColor = Config.NarrativeUI.DimmedContentColor;
                    }
                    
                    _terminal.Text(_layout.CONTENT_START_X, currentY, renderedLine.Text, outcomeColor, Config.NarrativeUI.BackgroundColor);
                    break;
                    
                case LineType.Empty:
                case LineType.Separator:
                    // Just skip (already cleared)
                    break;
            }
            
            currentY++;
        }
    }
    
    /// <summary>
    /// Render a history line (from previous narration nodes) in dark gray.
    /// No interactivity (keywords/actions are not clickable).
    /// </summary>
    private void RenderHistoryLine(RenderedLine line, int y)
    {
        Vector4 historyColor = Config.NarrativeUI.HistoryColor;
        
        switch (line.Type)
        {
            case LineType.Separator:
                // Render separator in slightly brighter color
                _terminal.Text(_layout.CONTENT_START_X, y, line.Text, Config.NarrativeUI.SeparatorColor, Config.NarrativeUI.BackgroundColor);
                break;
                
            case LineType.Empty:
                // Nothing to render
                break;
                
            default:
                // Render all other history lines in dark gray
                _terminal.Text(_layout.CONTENT_START_X, y, line.Text, historyColor, Config.NarrativeUI.BackgroundColor);
                break;
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
        int thinkingAttemptsRemaining,
        KeywordRegion? hoveredKeyword,
        bool dimContent = false,
        NarrationBlock? sourceBlock = null,
        List<int>? keywordOccurrenceIndices = null)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if (keywords == null || keywords.Count == 0)
        {
            // No keywords, just render normal text
            Vector4 textColor = dimContent ? Config.NarrativeUI.DimmedContentColor : Config.NarrativeUI.NarrativeColor;
            _terminal.Text(startX, y, text, textColor, Config.NarrativeUI.BackgroundColor);
            return;
        }

        // Use occurrence-aware parsing when indices are provided, otherwise highlight all occurrences
        var segments = keywordOccurrenceIndices != null
            ? _keywordRenderer.ParseNarrationWithKeywordsAtOccurrences(text, keywords, keywordOccurrenceIndices)
            : _keywordRenderer.ParseNarrationWithKeywords(text, keywords);
        
        int currentX = startX;
        
        foreach (var segment in segments)
        {
            if (segment.IsKeyword)
            {
                // Only highlight keywords if thinking attempts remain and content is not dimmed
                if (thinkingAttemptsRemaining > 0 && !dimContent)
                {
                    // Track keyword region for click detection, including source block for modusMentis chain
                    var keywordRegion = new KeywordRegion(
                        segment.KeywordValue!,
                        y,
                        currentX,
                        currentX + segment.Text.Length - 1,
                        sourceBlock,
                        sourceBlock?.KeywordContextMap?.GetValueOrDefault(segment.KeywordValue!));
                    _keywordRegions.Add(keywordRegion);
                    
                    // Check if this specific region is hovered
                    bool isHovered = hoveredKeyword != null &&
                                   hoveredKeyword.Y == y &&
                                   hoveredKeyword.StartX == currentX &&
                                   hoveredKeyword.EndX == currentX + segment.Text.Length - 1;
                    
                    Vector4 keywordColor = isHovered ? Config.NarrativeUI.KeywordHoverColor : Config.NarrativeUI.KeywordNormalColor;
                    Vector4 backgroundColor = isHovered ? Config.NarrativeUI.KeywordHoverBackgroundColor : Config.NarrativeUI.BackgroundColor;
                    _terminal.Text(currentX, y, segment.Text, keywordColor, backgroundColor);
                }
                else
                {
                    // No attempts remaining or content is dimmed - render as dimmed text
                    Vector4 textColor = dimContent ? Config.NarrativeUI.DimmedContentColor : Config.NarrativeUI.NarrativeColor;
                    _terminal.Text(currentX, y, segment.Text, textColor, Config.NarrativeUI.BackgroundColor);
                }
            }
            else
            {
                // Render normal text
                Vector4 textColor = dimContent ? Config.NarrativeUI.DimmedContentColor : Config.NarrativeUI.NarrativeColor;
                _terminal.Text(currentX, y, segment.Text, textColor, Config.NarrativeUI.BackgroundColor);
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
            if (region.Contains(mouseX, mouseY))
            {
                return region;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Render a single action line (actions are pre-wrapped in scroll buffer).
    /// </summary>
    private void RenderActionLine(string text, ParsedNarrativeAction action, int actionIndex, int y, int lineIndex, ActionRegion? hoveredAction, bool dimContent = false)
    {
        // Check if this action is hovered
        bool isHovered = hoveredAction != null && hoveredAction.ActionIndex == actionIndex;
        
        // Calculate colors - when dimmed, use dark grey regardless of hover state
        Vector4 prefixColor = dimContent ? Config.NarrativeUI.DimmedContentColor : Config.NarrativeUI.NarrativeColor;
        Vector4 textColor = dimContent ? Config.NarrativeUI.DimmedContentColor : 
            (isHovered ? Config.NarrativeUI.ActionHoverColor : Config.NarrativeUI.ActionNormalColor);
        Vector4 backgroundColor = dimContent ? Config.NarrativeUI.BackgroundColor :
            (isHovered ? Config.NarrativeUI.ActionHoverBackgroundColor : Config.NarrativeUI.BackgroundColor);
        
        // ModusMentis bracket colors - when dimmed, use dark grey; otherwise use hover-aware colors
        Vector4 modusMentisBracketColor = dimContent ? Config.NarrativeUI.DimmedContentColor :
            (isHovered ? Config.NarrativeUI.ActionHoverColor : Config.Colors.DarkYellowGrey);
        
        // ModusMentis level color - when an action is hovered, only highlight modusMentis levels in the chain
        Vector4 modusMentisLevelColor;
        if (dimContent)
        {
            modusMentisLevelColor = Config.NarrativeUI.DimmedContentColor;
        }
        else if (isHovered)
        {
            // This action is hovered - its modusMentis is always in its own chain, so highlight it
            modusMentisLevelColor = Config.NarrativeUI.LoadingColor;
        }
        else if (hoveredAction?.Action != null)
        {
            // Another action is hovered - check if this specific action element is in that chain
            // (different actions are never in each other's chains)
            bool isInChain = hoveredAction.Action.IsElementInChain(action);
            modusMentisLevelColor = isInChain ? Config.NarrativeUI.LoadingColor : Config.NarrativeUI.ModusMentisHeaderColor;
        }
        else
        {
            modusMentisLevelColor = Config.NarrativeUI.LoadingColor;
        }
        
        Vector4 modusMentisBracketBackground = backgroundColor; // Use action background for modusMentis parts too
        
        int startX = _layout.CONTENT_START_X;
        
        if (lineIndex == 0)
        {
            // First line: render difficulty glyph prefix + modusMentis bracket
            char diffChar = action.DifficultyLevel > 0
                ? Config.Symbols.DifficultyGlyphs[Math.Clamp(action.DifficultyLevel, 1, 10) - 1]
                : '>';
            string diffPrefix = $"{diffChar} ";

            // Build modusMentis bracket with level indicators
            string modusMentisName = action.ChainModusMentis?.DisplayName ?? action.ActionModusMentisId;
            int modusMentisLevel = action.ChainModusMentis?.Level ?? 1;
            string levelIndicators = new string(Config.Symbols.ModusMentisLevelIndicator, modusMentisLevel);

            // Glyph always uses difficulty-mapped color (dimmed in history)
            Vector4 diffGlyphColor = dimContent
                ? Config.NarrativeUI.DimmedContentColor
                : (action.DifficultyLevel > 0
                    ? Config.Symbols.DifficultyLevelColor(action.DifficultyLevel)
                    : modusMentisBracketColor);

            _terminal.Text(startX, y, diffPrefix, diffGlyphColor, Config.NarrativeUI.BackgroundColor);
            startX += diffPrefix.Length;
            
            // Render modusMentis bracket parts with hover-aware colors and backgrounds
            _terminal.Text(startX, y, "[", modusMentisBracketColor, modusMentisBracketBackground);
            startX += 1;
            
            _terminal.Text(startX, y, modusMentisName, modusMentisBracketColor, modusMentisBracketBackground);
            startX += modusMentisName.Length;
            
            _terminal.Text(startX, y, " ", modusMentisBracketColor, modusMentisBracketBackground);
            startX += 1;
            
            _terminal.Text(startX, y, levelIndicators, modusMentisLevelColor, modusMentisBracketBackground);
            startX += levelIndicators.Length;
            
            _terminal.Text(startX, y, "] ", modusMentisBracketColor, modusMentisBracketBackground);
            startX += 2;
            
            // Calculate available width for action text (respect right margin for scrollbar)
            int maxTextWidth = _layout.CONTENT_END_X - startX;
            string truncatedText = text.Length > maxTextWidth ? text.Substring(0, maxTextWidth) : text;
            
            _terminal.Text(startX, y, truncatedText, textColor, backgroundColor);
            
            // Track action region (will be updated as we encounter more lines)
            // Include the action reference for modusMentis chain access
            var actionRegion = new ActionRegion(actionIndex, y, y, _layout.CONTENT_START_X, _layout.CONTENT_END_X, action);
            _actionRegions.Add(actionRegion);
        }
        else
        {
            // Continuation line: indent by 4 spaces
            int continuationIndent = _layout.CONTENT_START_X + 4;
            
            // Calculate available width for continuation text (respect right margin)
            int maxTextWidth = _layout.CONTENT_END_X - continuationIndent;
            string truncatedText = text.Length > maxTextWidth ? text.Substring(0, maxTextWidth) : text;
            
            _terminal.Text(continuationIndent, y, truncatedText, textColor, backgroundColor);
            
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
                        _layout.CONTENT_START_X,
                        _layout.CONTENT_END_X,
                        lastRegion.Action  // Keep the action reference
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
            if (region.Contains(mouseX, mouseY))
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
    /// <summary>Render the proportional scrollbar for a <see cref="NarrationScrollBuffer"/>.</summary>
    public (int StartY, int Height) RenderScrollbar(
        NarrationScrollBuffer scrollBuffer,
        int scrollOffset,
        bool isThumbHovered)
        => RenderScrollbar(scrollBuffer.TotalLines, scrollOffset, isThumbHovered);
    
    /// <summary>
    /// Check if mouse is over scrollbar thumb.
    /// </summary>
    public bool IsMouseOverScrollbarThumb(int mouseX, int mouseY, (int StartY, int Height) thumb)
        => base.IsMouseOverScrollbarThumb(mouseX, mouseY, thumb);
    
    /// <summary>
    /// Check if mouse is over scrollbar track (but not thumb).
    /// </summary>
    public bool IsMouseOverScrollbarTrack(int mouseX, int mouseY, (int StartY, int Height) thumb)
        => base.IsMouseOverScrollbarTrack(mouseX, mouseY, thumb);
    
    /// <summary>
    /// Calculate scroll offset from mouse Y position on scrollbar.
    /// </summary>
    public int CalculateScrollOffsetFromMouseY(int mouseY, NarrationScrollBuffer scrollBuffer)
        => CalculateScrollOffsetFromMouseY(mouseY, scrollBuffer.TotalLines);
    
    /// <summary>
    /// Render the status bar at the bottom.
    /// When hovering over an action, the dice count portion is highlighted in yellow.
    /// </summary>
    public void RenderStatusBar(string message = "", ParsedNarrativeAction? hoveredAction = null)
    {
        int statusY = _layout.STATUS_BAR_Y;
        int separatorY = _layout.SEPARATOR_Y;
        
        // Draw separator line above status bar
        DrawHorizontalLine(separatorY);
        
        if (string.IsNullOrEmpty(message))
        {
            message = "Hover keywords to highlight • Click keywords to think (3 attempts remaining)";
        }
        
        // Truncate if too long
        int maxMessageWidth = _layout.CONTENT_WIDTH - 2;
        if (message.Length > maxMessageWidth)
        {
            message = message.Substring(0, maxMessageWidth - 3) + "...";
        }
        
        // If hovering over an action, render with highlighted dice count
        if (hoveredAction != null)
        {
            int totalDice = hoveredAction.GetTotalModusMentisLevel();
            string diceText = $"{totalDice}{Config.Symbols.ModusMentisLevelIndicator}";
            
            // Find where the dice text appears in the message
            int diceIndex = message.IndexOf(diceText);
            if (diceIndex >= 0)
            {
                // Render before dice text in dark gray
                string beforeDice = message.Substring(0, diceIndex);
                _terminal.Text(_layout.CONTENT_START_X, statusY, beforeDice, Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);
                
                // Render dice text in yellow (highlighted)
                int diceX = _layout.CONTENT_START_X + beforeDice.Length;
                _terminal.Text(diceX, statusY, diceText, Config.NarrativeUI.LoadingColor, Config.NarrativeUI.BackgroundColor);
                
                // Render after dice text in dark gray
                string afterDice = message.Substring(diceIndex + diceText.Length);
                int afterX = diceX + diceText.Length;
                _terminal.Text(afterX, statusY, afterDice, Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);
            }
            else
            {
                // Fallback: render entire message in status bar color
                _terminal.Text(_layout.CONTENT_START_X, statusY, message, Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);
            }
        }
        else
        {
            _terminal.Text(_layout.CONTENT_START_X, statusY, message, Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);
        }
    }
    
    // ShowLoadingIndicator — inherited from TerminalPanelUI (public virtual)

    
    /// <summary>
    /// Show dice roll loading indicator with animated rolling dice.
    /// </summary>

    // ShowError — inherited from TerminalPanelUI (public virtual)

    
    // DrawHorizontalLine, GenerateProgressBar, WrapText — inherited from TerminalPanelUI
    
    /// <summary>
    /// Render the "Continue" button at the bottom of the screen.
    /// Returns the button region for click detection.
    /// </summary>
    public (int X, int Y, int Width) RenderContinueButton(bool isHovered = false)
    {
        string buttonText = "[ Continue ]";
        int buttonWidth = buttonText.Length;
        int buttonX = (_layout.TERMINAL_WIDTH - buttonWidth) / 2;
        int buttonY = _layout.SEPARATOR_Y - 2; // Place near bottom, above separator
        
        Vector4 buttonColor = isHovered ? Config.NarrativeUI.ContinueButtonHoverColor : Config.NarrativeUI.ContinueButtonColor;
        Vector4 buttonBackgroundColor = isHovered ? Config.NarrativeUI.ContinueButtonHoverBackgroundColor : Config.NarrativeUI.ContinueButtonBackgroundColor;
        
        _terminal.Text(buttonX, buttonY, buttonText, buttonColor, buttonBackgroundColor);
        
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
