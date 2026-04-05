using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// Manages scrollable narration blocks with viewport rendering.
/// Stores full narration history and renders only visible portion.
/// Supports keeping previous narration nodes as grayed-out history.
/// </summary>
public class NarrationScrollBuffer
{
    private readonly List<NarrationBlock> _blocks = new();
    private readonly List<RenderedLine> _renderedLines = new();
    private readonly List<RenderedLine> _historyLines = new(); // Previous narration node lines (grayed out)
    private int _scrollOffset = 0;
    private readonly int _maxWidth;
    private readonly NarrativeLayout _layout;

    public int ScrollOffset => _scrollOffset;
    public int TotalLines => _renderedLines.Count;
    
    /// <summary>
    /// Number of lines that are history (from previous narration nodes).
    /// </summary>
    public int HistoryLineCount => _historyLines.Count;

    public NarrationScrollBuffer(int maxWidth, NarrativeLayout layout)
    {
        _maxWidth = maxWidth;
        _layout = layout ?? throw new ArgumentNullException(nameof(layout));
    }

    /// <summary>
    /// Add a narration block and re-render all lines.
    /// Applies truncation cleanup to the block's text if it appears incomplete.
    /// </summary>
    public void AddBlock(NarrationBlock block)
    {
        // Speaking blocks are manually assembled with outer quotes — skip truncation cleanup
        // because CleanTruncatedText would strip the closing " (it only recognises . ! ? as endings).
        string cleanedText = block.Type == NarrationBlockType.Speaking
            ? block.Text
            : TextTruncationUtils.CleanTruncatedText(block.Text);
        
        // Create a new block with cleaned text if it was modified, preserving all properties
        var blockToAdd = cleanedText != block.Text
            ? new NarrationBlock(block.Type, block.ModusMentis, cleanedText, block.Keywords, block.Actions, block.ChainOrigin,
                block.SourceObservationType, block.LinkedOutcome, block.KeywordOutcomeMap, block.Sentences, block.KeywordContextMap, block.SpeakerName)
            : block;
        
        _blocks.Add(blockToAdd);
        RegenerateRenderedLines();
        
        // Don't auto-scroll - let user see from the top
        // User can scroll down if needed
    }

    /// <summary>
    /// Get visible lines for the viewport.
    /// </summary>
    public List<RenderedLine> GetVisibleLines(int startLine, int visibleCount)
    {
        if (_renderedLines.Count == 0)
            return new List<RenderedLine>();

        // Start from current scroll offset
        int actualStart = Math.Max(0, Math.Min(_scrollOffset, _renderedLines.Count - 1));
        int actualCount = Math.Min(visibleCount, _renderedLines.Count - actualStart);

        if (actualCount <= 0)
            return new List<RenderedLine>();

        return _renderedLines.Skip(actualStart).Take(actualCount).ToList();
    }

    /// <summary>
    /// Scroll up by specified number of lines.
    /// </summary>
    public void ScrollUp(int lines = 1)
    {
        if (_scrollOffset <= 0)
            return;
        
        _scrollOffset = Math.Max(0, _scrollOffset - lines);
    }

    /// <summary>
    /// Scroll down by specified number of lines.
    /// </summary>
    public void ScrollDown(int lines = 1)
    {
        int maxScroll = _layout.CalculateMaxScrollOffset(_renderedLines.Count);
        if (_scrollOffset >= maxScroll)
            return;
        
        _scrollOffset = Math.Min(maxScroll, _scrollOffset + lines);
    }

    /// <summary>
    /// Scroll to the bottom of the buffer.
    /// </summary>
    public void ScrollToBottom()
    {
        _scrollOffset = Math.Max(0, _layout.CalculateMaxScrollOffset(_renderedLines.Count));
    }

    /// <summary>
    /// Set the scroll offset directly (for scrollbar dragging).
    /// </summary>
    public void SetScrollOffset(int offset)
    {
        int maxScroll = _layout.CalculateMaxScrollOffset(_renderedLines.Count);
        _scrollOffset = Math.Clamp(offset, 0, Math.Max(0, maxScroll));
    }

    /// <summary>
    /// Can we scroll up?
    /// </summary>
    public bool CanScrollUp() => _scrollOffset > 0;

    /// <summary>
    /// Can we scroll down?
    /// </summary>
    public bool CanScrollDown(int visibleLines) => _scrollOffset + visibleLines < _renderedLines.Count;

    /// <summary>
    /// Clear all blocks and rendered lines (including history).
    /// </summary>
    public void Clear()
    {
        _blocks.Clear();
        _renderedLines.Clear();
        _historyLines.Clear();
        _scrollOffset = 0;
    }
    
    /// <summary>
    /// Convert current narration content to history (grayed out, non-interactive).
    /// This preserves the text for player reference while starting a new narration node.
    /// Adds a visual separator line at the end.
    /// </summary>
    public void ConvertToHistory()
    {
        Console.WriteLine($"ConvertToHistory: Starting with {_renderedLines.Count} rendered lines, {_historyLines.Count} existing history lines");
        
        if (_renderedLines.Count == 0)
        {
            Console.WriteLine("ConvertToHistory: No lines to convert, returning");
            return;
        }
        
        int convertedCount = 0;
        
        // Only convert lines that are NOT already history
        // (_renderedLines contains both history lines at the start and current content after)
        foreach (var line in _renderedLines)
        {
            // Skip lines that are already in history
            if (line.IsHistory)
                continue;
                
            // Create new line with IsHistory=true, and clear interactive elements
            var historyLine = new RenderedLine(
                Text: line.Text,
                Type: line.Type,
                BlockType: line.BlockType,
                Keywords: null,  // Remove keywords to disable interactivity
                Actions: null,   // Remove actions to disable interactivity
                IsHistory: true,
                GlobalActionIndex: -1,
                SourceBlock: null  // Clear source block for history
            );
            _historyLines.Add(historyLine);
            convertedCount++;
        }
        
        Console.WriteLine($"ConvertToHistory: Converted {convertedCount} non-history lines");
        
        // Add separator line
        string separatorText = new string('─', Math.Min(_maxWidth, 40)); // 40 dashes or max width
        _historyLines.Add(new RenderedLine(
            Text: separatorText,
            Type: LineType.Separator,
            BlockType: NarrationBlockType.Observation, // Doesn't matter for separator
            Keywords: null,
            Actions: null,
            IsHistory: true,
            GlobalActionIndex: -1,
            SourceBlock: null
        ));
        
        // Add empty line after separator for spacing
        _historyLines.Add(new RenderedLine(
            Text: "",
            Type: LineType.Empty,
            BlockType: NarrationBlockType.Observation,
            Keywords: null,
            Actions: null,
            IsHistory: true,
            GlobalActionIndex: -1,
            SourceBlock: null
        ));
        
        // Clear current blocks (they're now in history)
        _blocks.Clear();
        _renderedLines.Clear();
        
        // Regenerate (will include history lines at the top)
        RegenerateRenderedLines();
        
        // Scroll so the last portion of history is visible and new content will appear at bottom
        _scrollOffset = Math.Max(0, _historyLines.Count - _layout.NARRATIVE_HEIGHT);
        
        Console.WriteLine($"ConvertToHistory: Complete - {_historyLines.Count} history lines, {_renderedLines.Count} total lines, scroll offset: {_scrollOffset}");
    }

    /// <summary>
    /// Regenerate all rendered lines from blocks with word wrapping.
    /// History lines are prepended at the top.
    /// </summary>
    private void RegenerateRenderedLines()
    {
        _renderedLines.Clear();
        
        // First, add all history lines (from previous narration nodes)
        _renderedLines.AddRange(_historyLines);
        
        // Track global action index across all thinking blocks
        int globalActionIndex = 0;

        foreach (var block in _blocks)
        {
            // Add modusMentis name header (if present)
            if (block.ModusMentis != null)
            {
                // Generate modusMentis level indicators using dice glyphs
                string levelIndicators = new string(Config.Symbols.ModusMentisLevelIndicator, block.ModusMentis.Level);
                string headerText = block.Type == NarrationBlockType.Speaking && block.SpeakerName != null
                    ? $"[{block.SpeakerName.ToUpper()}/{block.ModusMentis.DisplayName.ToUpper()} {levelIndicators}]"
                    : $"[{block.ModusMentis.DisplayName.ToUpper()} {levelIndicators}]";
                
                _renderedLines.Add(new RenderedLine(
                    Text: headerText,
                    Type: LineType.Header,
                    BlockType: block.Type,
                    Keywords: null,
                    Actions: null,
                    IsHistory: false,
                    GlobalActionIndex: -1,
                    SourceBlock: block
                ));
                
                // Empty line after header
                _renderedLines.Add(new RenderedLine(
                    Text: "",
                    Type: LineType.Empty,
                    BlockType: block.Type,
                    Keywords: null,
                    Actions: null,
                    IsHistory: false,
                    GlobalActionIndex: -1,
                    SourceBlock: block
                ));
            }

            // Wrap and add narration content.
            // Determine line type based on block type
            LineType lineType = block.Type switch
            {
                NarrationBlockType.Action => LineType.Action,
                NarrationBlockType.Outcome => LineType.Outcome,
                NarrationBlockType.Speaking => LineType.Content,
                _ => LineType.Content
            };

            // Wrap block.Text as one continuous paragraph (preserving natural word-flow).
            // When per-sentence data is available, compute which character range each sentence
            // occupies in block.Text and assign each wrapped line only the keywords from the
            // sentence(s) that overlap its character range — preventing cross-sentence highlighting.
            if (block.Sentences != null && block.Sentences.Count > 0)
            {
                var sentenceRanges = ComputeSentenceRanges(block.Text, block.Sentences);
                var linesWithOffsets = WrapTextWithOffsets(block.Text, _maxWidth);
                foreach (var (lineText, lineStart) in linesWithOffsets)
                {
                    int lineEnd = lineStart + lineText.Length;
                    var lineKeywords = new List<string>();
                    var lineOccurrences = new List<int>();
                    foreach (var (sentStart, sentEnd, kwsWithOffsets) in sentenceRanges)
                    {
                        if (sentStart >= lineEnd || sentEnd <= lineStart) continue;
                        foreach (var (kw, absOffset) in kwsWithOffsets)
                        {
                            if (absOffset >= lineStart && absOffset < lineEnd)
                            {
                                // Count how many times kw appears in the line before this position
                                int occIndexInLine = CountOccurrencesUpTo(lineText, kw, absOffset - lineStart);
                                lineKeywords.Add(kw);
                                lineOccurrences.Add(occIndexInLine);
                            }
                        }
                    }
                    _renderedLines.Add(new RenderedLine(
                        Text: lineText,
                        Type: lineType,
                        BlockType: block.Type,
                        Keywords: lineKeywords.Count > 0 ? lineKeywords : null,
                        Actions: null,
                        IsHistory: false,
                        GlobalActionIndex: -1,
                        SourceBlock: block,
                        KeywordOccurrenceIndices: lineKeywords.Count > 0 ? lineOccurrences : null
                    ));
                }
            }
            else
            {
                // Fallback for blocks without sentence data: wrap whole text with all keywords.
                var wrappedLines = WrapText(block.Text, _maxWidth);
                foreach (var line in wrappedLines)
                {
                    _renderedLines.Add(new RenderedLine(
                        Text: line,
                        Type: lineType,
                        BlockType: block.Type,
                        Keywords: block.Keywords,
                        Actions: null,
                        IsHistory: false,
                        GlobalActionIndex: -1,
                        SourceBlock: block
                    ));
                }
            }

            // Add action lines if this is a Thinking block
            if (block.Type == NarrationBlockType.Thinking && block.Actions != null && block.Actions.Count > 0)
            {
                // Add empty line before actions
                _renderedLines.Add(new RenderedLine(
                    Text: "",
                    Type: LineType.Empty,
                    BlockType: block.Type,
                    Keywords: null,
                    Actions: null,
                    IsHistory: false,
                    GlobalActionIndex: -1,
                    SourceBlock: block
                ));
                
                // Pre-wrap each action to match actual rendered lines
                foreach (var action in block.Actions)
                {
                    // Calculate wrapped lines for this action
                    // Format: "> [ModusMentisName ◼◼◼] action text" - need to account for level indicators
                    string prefix = "> ";
                    string modusMentisName = action.ChainModusMentis?.DisplayName ?? action.ActionModusMentisId;
                    int modusMentisLevel = action.ChainModusMentis?.Level ?? 1;
                    string levelIndicators = new string(Config.Symbols.ModusMentisLevelIndicator, modusMentisLevel);
                    string fullModusMentisBracket = $"[{modusMentisName} {levelIndicators}] ";
                    
                    int firstLinePrefix = prefix.Length + fullModusMentisBracket.Length;
                    int firstLineWidth = _maxWidth - firstLinePrefix;
                    int continuationWidth = _maxWidth - 4; // 4-space indent
                    
                    var wrappedActionLines = WrapActionText(action.DisplayText, firstLineWidth, continuationWidth);
                    
                    // Store the global action index for this action
                    int thisActionIndex = globalActionIndex;
                    globalActionIndex++;
                    
                    // Add a RenderedLine for each wrapped line of this action
                    for (int i = 0; i < wrappedActionLines.Count; i++)
                    {
                        _renderedLines.Add(new RenderedLine(
                            Text: wrappedActionLines[i],
                            Type: LineType.Action,
                            BlockType: block.Type,
                            Keywords: null,
                            Actions: i == 0 ? new List<ParsedNarrativeAction> { action } : null, // Only first line has action reference
                            IsHistory: false,
                            GlobalActionIndex: thisActionIndex,  // Store global index for all wrapped lines of this action
                            SourceBlock: block
                        ));
                    }
                }
            }

            // Empty line after block
            _renderedLines.Add(new RenderedLine(
                Text: "",
                Type: LineType.Empty,
                BlockType: block.Type,
                Keywords: null,
                Actions: null,
                IsHistory: false,
                GlobalActionIndex: -1,
                SourceBlock: block
            ));
        }
    }

    /// <summary>
    /// Computes the character range [start, end) of each sentence within blockText,
    /// and for each keyword finds its absolute char offset within blockText (first occurrence
    /// inside the sentence range). Returns (start, end, [(keyword, absOffset)]) per sentence.
    /// </summary>
    private static List<(int Start, int End, List<(string Keyword, int AbsOffset)> Keywords)> ComputeSentenceRanges(
        string blockText, List<NarrationSentence> sentences)
    {
        var ranges = new List<(int, int, List<(string, int)>)>();
        int searchFrom = 0;
        foreach (var sentence in sentences)
        {
            int idx = blockText.IndexOf(sentence.Text, searchFrom, StringComparison.Ordinal);
            if (idx < 0)
                idx = blockText.IndexOf(sentence.Text, StringComparison.Ordinal);
            if (idx < 0) continue;

            int sentEnd = idx + sentence.Text.Length;
            var kwsWithOffsets = new List<(string, int)>();
            foreach (var kw in sentence.Keywords)
            {
                int kwPos = blockText.IndexOf(kw, idx, sentEnd - idx, StringComparison.OrdinalIgnoreCase);
                if (kwPos >= 0)
                    kwsWithOffsets.Add((kw, kwPos));
            }
            ranges.Add((idx, sentEnd, kwsWithOffsets));
            searchFrom = sentEnd;
        }
        return ranges;
    }

    /// <summary>
    /// Counts how many times <paramref name="keyword"/> appears (case-insensitive substring)
    /// in <paramref name="text"/> strictly before <paramref name="upToIndex"/>.
    /// Used to compute the occurrence index of a keyword within a single rendered line.
    /// </summary>
    private static int CountOccurrencesUpTo(string text, string keyword, int upToIndex)
    {
        int count = 0;
        int pos = 0;
        while (pos < upToIndex)
        {
            int found = text.IndexOf(keyword, pos, StringComparison.OrdinalIgnoreCase);
            if (found < 0 || found >= upToIndex) break;
            count++;
            pos = found + keyword.Length;
        }
        return count;
    }

    /// <summary>
    /// Same word-wrap logic as WrapText but also returns the start character offset of each
    /// line within the original text, so callers can map lines back to sentence ranges.
    /// </summary>
    private List<(string Line, int StartOffset)> WrapTextWithOffsets(string text, int maxWidth)
    {
        var result = new List<(string, int)>();
        if (string.IsNullOrEmpty(text)) { result.Add(("", 0)); return result; }

        int globalOffset = 0;
        var paragraphs = text.Split(new[] { '\n', '\r' }, StringSplitOptions.None);

        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                result.Add(("", globalOffset));
                globalOffset += paragraph.Length + 1;
                continue;
            }

            var sb = new StringBuilder();
            int lineStartInParagraph = 0;
            int pos = 0;

            while (pos < paragraph.Length)
            {
                // Skip spaces
                while (pos < paragraph.Length && paragraph[pos] == ' ') pos++;
                if (pos >= paragraph.Length) break;

                int wordStart = pos;
                while (pos < paragraph.Length && paragraph[pos] != ' ') pos++;
                string word = paragraph.Substring(wordStart, pos - wordStart);

                string testLine = sb.Length == 0 ? word : sb + " " + word;
                if (testLine.Length <= maxWidth)
                {
                    if (sb.Length == 0) lineStartInParagraph = wordStart;
                    else sb.Append(' ');
                    sb.Append(word);
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        result.Add((sb.ToString(), globalOffset + lineStartInParagraph));
                        sb.Clear();
                    }
                    if (word.Length > maxWidth)
                    {
                        result.Add((word[..maxWidth], globalOffset + wordStart));
                        sb.Append(word[maxWidth..]);
                        lineStartInParagraph = wordStart + maxWidth;
                    }
                    else
                    {
                        lineStartInParagraph = wordStart;
                        sb.Append(word);
                    }
                }
            }

            if (sb.Length > 0)
                result.Add((sb.ToString(), globalOffset + lineStartInParagraph));

            globalOffset += paragraph.Length + 1;
        }

        return result;
    }

    /// <summary>
    /// Wrap text at word boundaries.
    /// </summary>
    private List<string> WrapText(string text, int maxWidth)
    {
        var lines = new List<string>();
        
        if (string.IsNullOrEmpty(text))
        {
            lines.Add("");
            return lines;
        }

        var paragraphs = text.Split(new[] { '\n', '\r' }, StringSplitOptions.None);

        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                lines.Add("");
                continue;
            }

            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var currentLine = new StringBuilder();

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
                    // Line would be too long, start new line
                    if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                    }

                    // If single word is too long, force it on its own line
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
    /// Wrap action text with different widths for first line and continuation lines.
    /// Matches the wrapping logic in Phase6ObservationUI.
    /// </summary>
    private List<string> WrapActionText(string text, int firstLineWidth, int continuationWidth)
    {
        var lines = new List<string>();
        
        if (string.IsNullOrEmpty(text))
        {
            lines.Add("");
            return lines;
        }
        
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = new StringBuilder();
        int currentMaxWidth = firstLineWidth;
        
        foreach (var word in words)
        {
            var testLine = currentLine.Length == 0 ? word : currentLine + " " + word;
            
            if (testLine.Length <= currentMaxWidth)
            {
                if (currentLine.Length > 0)
                    currentLine.Append(' ');
                currentLine.Append(word);
            }
            else
            {
                // Line would be too long, start new line
                if (currentLine.Length > 0)
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                    currentMaxWidth = continuationWidth;
                }
                
                // If single word is too long, force it on its own line
                if (word.Length > currentMaxWidth)
                {
                    lines.Add(word.Substring(0, currentMaxWidth));
                    currentLine.Append(word.Substring(currentMaxWidth));
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
        
        return lines;
    }

    /// <summary>
    /// Get all blocks for external access.
    /// </summary>
    public IReadOnlyList<NarrationBlock> GetBlocks() => _blocks.AsReadOnly();
}

/// <summary>
/// A single rendered line with metadata.
/// </summary>
public record RenderedLine(
    string Text,
    LineType Type,
    NarrationBlockType BlockType,
    List<string>? Keywords,
    List<ParsedNarrativeAction>? Actions,  // Actions for rendering (only for Action lines)
    bool IsHistory = false,  // True if this line is part of history (from previous narration nodes)
    int GlobalActionIndex = -1,  // Global action index (0-based) across all thinking blocks, -1 if not an action line
    NarrationBlock? SourceBlock = null,  // The narration block this line comes from (for modusMentis chain tracking)
    List<int>? KeywordOccurrenceIndices = null  // Parallel to Keywords: which occurrence (0-based) within this line to highlight
);

/// <summary>
/// Type of rendered line.
/// </summary>
public enum LineType
{
    Header,     // ModusMentis name header
    Content,    // Narration text
    Action,     // Action line (for Thinking blocks)
    Outcome,    // Outcome narration (for Action/Outcome blocks)
    Empty,      // Spacing
    Separator   // Transition separator between narration nodes
}
