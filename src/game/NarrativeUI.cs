using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene;

namespace Cathedral.Game;

/// <summary>
/// Renders Chain-of-Thought observation UI with scrollable narration,
/// highlighted keywords, and hover interactions.
/// </summary>
public class NarrativeUI : TerminalPanelUI
{
    private readonly KeywordRenderer _keywordRenderer;
    private List<KeywordRegion> _keywordRegions = new();
    private List<ActionRegion> _actionRegions = new();

    /// <summary>Clickable keyword regions in the current frame (used by --cli to list handles).</summary>
    public IReadOnlyList<KeywordRegion> KeywordRegions => _keywordRegions;

    /// <summary>Clickable action regions in the current frame (used by --cli to list handles).</summary>
    public IReadOnlyList<ActionRegion> ActionRegions => _actionRegions;
    
    public NarrativeUI(TerminalHUD terminal) : base(terminal)
    {
        _keywordRenderer = new KeywordRenderer();
        Console.WriteLine($"NarrativeUI: Initialized with {_terminal.Width}x{_terminal.Height} terminal (padding: T={_layout.TOP_PADDING} B={_layout.BOTTOM_PADDING} L={_layout.LEFT_PADDING} R={_layout.RIGHT_PADDING})");
    }
    
    /// <summary>
    /// Fallback maximum thinking attempts, used only when no member-derived value is available
    /// (e.g. before the active member is known). The real per-member value comes from the
    /// encephalon-derived <see cref="Cathedral.Game.Narrative.NoeticPointsStat"/> via
    /// <see cref="Cathedral.Game.Narrative.PartyMember.MaxNoeticPoints"/>.
    /// </summary>
    public static int GetMaxThinkingAttempts()
    {
        return 13;
    }
    
    /// <summary>Clear the entire terminal then reset keyword/action tracking regions.</summary>
    public override void Clear()
    {
        base.Clear();
        _keywordRegions.Clear();
    }
    
    /// <summary>
    /// Render the header: active agent name (left) and noetic-point counter (right).
    /// Pass <paramref name="showNoeticPoints"/> as false for phases where noetic points are
    /// not consumed (childhood reminescence, get-up scene).
    /// </summary>
    public void RenderHeader(string activeAgentName, int thinkingAttemptsRemaining, int maxNoeticPoints,
        bool showNoeticPoints = true)
    {
        int headerY = _layout.TOP_PADDING;

        // Left: agent name in uppercase brackets
        string agentLabel = $"[{activeAgentName.ToUpper()}]";
        _terminal.Text(_layout.CONTENT_START_X, headerY, agentLabel, Config.NarrativeUI.HeaderColor, Config.NarrativeUI.BackgroundColor);

        // Right: noetic-point counter (only when noetic points are meaningful), named. The markers
        // alone were a row of unexplained circles — the one number the whole narration loop is spent
        // against, with nothing on screen saying what it counts.
        if (showNoeticPoints)
        {
            int maxAttempts = maxNoeticPoints;
            const string label = "NOETIC ";
            string prefix = "[";
            // Reserve space: label + prefix "[" + maxAttempts markers + suffix "]"
            int suffixWidth = label.Length + 1 + maxAttempts + 1;
            int labelX  = _layout.CONTENT_END_X - suffixWidth;
            int prefixX = labelX + label.Length;

            _terminal.Text(labelX, headerY, label, Config.NarrativeUI.HeaderColor, Config.NarrativeUI.BackgroundColor);
            _terminal.Text(prefixX, headerY, prefix, Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);

            int markerX = prefixX + prefix.Length;
            for (int i = 0; i < maxAttempts; i++)
            {
                bool isRemaining = i < thinkingAttemptsRemaining;
                Vector4 markerColor = isRemaining
                    ? Config.NarrativeUI.LoadingColor
                    : Config.NarrativeUI.HistoryColor;
                _terminal.Text(markerX, headerY, Config.Symbols.NoeticPointMarker.ToString(), markerColor, Config.NarrativeUI.BackgroundColor);
                markerX++;
            }

            _terminal.Text(markerX, headerY, "]", Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);
        }

        // Separator line
        DrawHorizontalLine(_layout.TOP_PADDING + 1);
    }
    
    /// <summary>
    /// Render observation blocks with keywords highlighted.
    /// History lines (from previous narration nodes) are rendered in dark gray.
    /// </summary>
    public void RenderObservationBlocks(
        NarrationScrollBuffer scrollBuffer,
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
        
        // Get the visible window (the buffer owns the scroll position).
        // Subtract 1 from NARRATIVE_HEIGHT to account for the bottom separator line
        int visibleContentHeight = _layout.NARRATIVE_HEIGHT - _layout.SEPARATOR_HEIGHT;
        var visibleLines = scrollBuffer.GetVisibleLines(visibleContentHeight);
        
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
                    // Found an outcome line (narrative text or report chip); find the start of this block.
                    lastOutcomeBlockStart = i;
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

            // When an action is hovered, grey out every block outside its chain-of-thought
            // (full header, narration text and dice symbols) so the chain stands out.
            // Clickable action lines are excluded here: RenderActionLine dims per-action,
            // so sibling actions of the hovered one grey out individually.
            if (!shouldDimThisLine && hoveredAction?.Action != null &&
                renderedLine.SourceBlock != null &&
                !(renderedLine.Type == LineType.Action && renderedLine.Actions != null) &&
                !hoveredAction.Action.IsElementInChain(renderedLine.SourceBlock))
            {
                shouldDimThisLine = true;
            }
            
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
                        
                        // ModusMentis level indicator color: blocks outside a hovered action's chain
                        // are fully dimmed above, so any line reaching here un-dimmed is either
                        // in the chain or no action is hovered — both keep the bright dice color.
                        Vector4 modusMentisLevelColor = shouldDimThisLine
                            ? Config.NarrativeUI.DimmedContentColor
                            : Config.NarrativeUI.LoadingColor;
                        
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
                    // A dialogue reply the player chose: flat, in the player's colour. These carry
                    // no keywords, so the highlighting pass is skipped entirely.
                    if (renderedLine.BlockType == NarrationBlockType.PlayerSpeaking)
                    {
                        _terminal.Text(_layout.CONTENT_START_X, currentY, renderedLine.Text,
                            shouldDimThisLine ? Config.NarrativeUI.DimmedContentColor : Config.Colors.LightPurple,
                            Config.NarrativeUI.BackgroundColor);
                        break;
                    }

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
                        renderedLine.KeywordOccurrenceIndices,
                        renderedLine.KeywordAnchors);
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
                        // VerbAction result block (from VerbAction block type) - detect SUCCESS/FAILURE
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
                    
                case LineType.Report:
                    RenderReportChip(renderedLine, currentY,
                        shouldDimThisLine ? Config.NarrativeUI.DimmedContentColor : null);
                    break;

                case LineType.Separator:
                    // A segment rule that has not yet aged into history (ConvertToHistory marks its
                    // own separators as history, so this covers any live one).
                    _terminal.Text(_layout.CONTENT_START_X, currentY, renderedLine.Text,
                        Config.NarrativeUI.SeparatorColor, Config.NarrativeUI.BackgroundColor);
                    break;

                case LineType.DialogueOption:
                    // Dialogue reply lines are interactive only in the dialogue panel; if one is
                    // still live here (not yet aged into history), show the chosen reply in the
                    // player's colour and the rejected ones greyed, never clickable.
                    {
                        int sel = renderedLine.SourceBlock?.SelectedDialogueOptionIndex ?? -1;
                        Vector4 optColor = shouldDimThisLine ? Config.NarrativeUI.DimmedContentColor
                            : sel < 0                                    ? Config.NarrativeUI.NarrativeColor
                            : sel == renderedLine.DialogueOptionIndex    ? Config.Colors.LightPurple
                            :                                              Config.NarrativeUI.DimmedContentColor;
                        _terminal.Text(_layout.CONTENT_START_X, currentY, renderedLine.Text,
                            optColor, Config.NarrativeUI.BackgroundColor);
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
    /// Render a history line (from previous narration nodes) in dark gray.
    /// No interactivity (keywords/actions are not clickable).
    /// </summary>
    private void RenderHistoryLine(RenderedLine line, int y)
    {
        Vector4 historyColor = Config.NarrativeUI.HistoryColor;
        
        switch (line.Type)
        {
            case LineType.Separator:
                _terminal.Text(_layout.CONTENT_START_X, y, line.Text, Config.NarrativeUI.SeparatorColor, Config.NarrativeUI.BackgroundColor);
                break;

            case LineType.Report:
                // Report chips collapse to plain history grey — no color background in history.
                // (ConvertToHistory drops the Report reference, so this is the text-only fallback.)
                _terminal.Text(_layout.CONTENT_START_X, y, line.Text.TrimEnd(), historyColor, Config.NarrativeUI.BackgroundColor);
                break;

            case LineType.Empty:
                break;

            default:
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
        List<int>? keywordOccurrenceIndices = null,
        List<NarrativeAnchor?>? keywordAnchors = null)
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

        // Segments arrive in text order, so the Nth highlighted occurrence of a word on this line is
        // the entry whose recorded occurrence index is N. Matching on the word ALONE would be
        // ambiguous exactly where it now matters — two sentences about two men, wrapped onto one
        // line, both offering "man" and each acting on a different person.
        var seenPerKeyword = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var segment in segments)
        {
            if (segment.IsKeyword)
            {
                // Only highlight keywords if thinking attempts remain and content is not dimmed
                if (thinkingAttemptsRemaining > 0 && !dimContent)
                {
                    string kw = segment.KeywordValue!;
                    seenPerKeyword.TryGetValue(kw, out int occurrence);
                    seenPerKeyword[kw] = occurrence + 1;

                    // Track keyword region for click detection, including source block for modusMentis
                    // chain and the anchor this occurrence acts on.
                    var keywordRegion = new KeywordRegion(
                        kw,
                        y,
                        currentX,
                        currentX + segment.Text.Length - 1,
                        sourceBlock,
                        AnchorFor(kw, occurrence, keywords, keywordOccurrenceIndices, keywordAnchors));
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
    /// The anchor recorded for the <paramref name="occurrence"/>-th appearance of
    /// <paramref name="keyword"/> on this line. The three lists are parallel, so this finds the
    /// entry that matches on both word and occurrence index. Null when the block carries no
    /// per-sentence data, which leaves the click to fall back to the block's single anchor.
    /// </summary>
    private static NarrativeAnchor? AnchorFor(
        string keyword, int occurrence,
        List<string> keywords, List<int>? occurrenceIndices, List<NarrativeAnchor?>? anchors)
    {
        if (anchors == null || occurrenceIndices == null) return null;
        for (int i = 0; i < keywords.Count && i < occurrenceIndices.Count && i < anchors.Count; i++)
            if (occurrenceIndices[i] == occurrence
             && keywords[i].Equals(keyword, StringComparison.OrdinalIgnoreCase))
                return anchors[i];
        return null;
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
        // Actions judged impossible are always rendered greyed-out
        dimContent = dimContent || action.IsImpossible;

        // Check if this action is hovered
        bool isHovered = hoveredAction != null && hoveredAction.ActionIndex == actionIndex;

        // When another action is hovered, grey out this whole action line (difficulty glyph,
        // bracket, dice and text) — only the hovered action's chain stays lit.
        if (!isHovered && hoveredAction?.Action != null && !hoveredAction.Action.IsElementInChain(action))
        {
            dimContent = true;
        }

        // Calculate colors - when dimmed, use dark grey regardless of hover state
        Vector4 prefixColor = dimContent ? Config.NarrativeUI.DimmedContentColor : Config.NarrativeUI.NarrativeColor;
        Vector4 textColor = dimContent ? Config.NarrativeUI.DimmedContentColor : 
            (isHovered ? Config.NarrativeUI.ActionHoverColor : Config.NarrativeUI.ActionNormalColor);
        Vector4 backgroundColor = dimContent ? Config.NarrativeUI.BackgroundColor :
            (isHovered ? Config.NarrativeUI.ActionHoverBackgroundColor : Config.NarrativeUI.BackgroundColor);
        
        // ModusMentis bracket colors - when dimmed, use dark grey; otherwise use hover-aware colors
        Vector4 modusMentisBracketColor = dimContent ? Config.NarrativeUI.DimmedContentColor :
            (isHovered ? Config.NarrativeUI.ActionHoverColor : Config.Colors.DarkYellowGrey);
        
        // ModusMentis level color: actions outside a hovered action's chain are fully
        // dimmed above, so any un-dimmed action keeps the bright dice color.
        Vector4 modusMentisLevelColor = dimContent
            ? Config.NarrativeUI.DimmedContentColor
            : Config.NarrativeUI.LoadingColor;
        
        Vector4 modusMentisBracketBackground = backgroundColor; // Use action background for modusMentis parts too
        
        int startX = _layout.CONTENT_START_X;
        
        if (lineIndex == 0)
        {
            // First line: render difficulty glyph prefix + modusMentis bracket. The glyph choice
            // lives on the action so history lines (which bake the whole prefix into their text)
            // cannot drift from what is painted here.
            string diffPrefix = $"{action.DifficultyGlyph} ";

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
    /// Render the footer status bar. In normal operation, pass scene-info text (biome, location,
    /// time). For transient states (dice, error) pass the relevant status message.
    /// </summary>
    public void RenderStatusBar(string message = "")
        => DrawStatusBar(message);

    // RenderExitButton — inherited from TerminalPanelUI (shared with DialogueTreeUI)
}
