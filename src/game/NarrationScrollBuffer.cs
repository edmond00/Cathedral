using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    private readonly List<RenderedLine> _historyLines = new(); // Previous phase/node lines (grayed out)
    private int _scrollOffset = 0;
    private readonly int _maxWidth;
    private readonly NarrativeLayout _layout;

    /// <summary>
    /// Guards every read/write of the line lists. Blocks are appended from LLM continuations on
    /// background tasks while the render thread reads the visible window every frame, and
    /// <see cref="RegenerateRenderedLines"/> clears-then-refills <see cref="_renderedLines"/> —
    /// without this an enumeration can observe the empty intermediate state.
    /// </summary>
    private readonly object _gate = new();

    public int ScrollOffset => _scrollOffset;
    public int TotalLines { get { lock (_gate) return _renderedLines.Count; } }

    /// <summary>
    /// Number of lines that are history (from previous phases or narration nodes).
    /// </summary>
    public int HistoryLineCount { get { lock (_gate) return _historyLines.Count; } }

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
        // Speech blocks are manually assembled with outer quotes and multi-line blocks (e.g. a
        // combat log) carry their own line breaks — both would be mangled by CleanTruncatedText,
        // which only recognises . ! ? as sentence endings and would strip everything after the last one.
        // Bracketed system chips ("[The conversation has ended.]", affinity notes) are hand-written,
        // never LLM-truncated, and end in ']' — cleanup would eat their tail and append "...".
        bool isSystemChip = block.Text.Length >= 2 && block.Text[0] == '[' && block.Text[^1] == ']';
        bool skipCleanup = block.Type is NarrationBlockType.Speaking or NarrationBlockType.PlayerSpeaking
                                      or NarrationBlockType.DialogueOptions
                           || block.Text.Contains('\n')
                           || isSystemChip;
        string cleanedText = skipCleanup ? block.Text : TextTruncationUtils.CleanTruncatedText(block.Text);

        // Create a new block with cleaned text if it was modified, preserving all properties
        var blockToAdd = cleanedText != block.Text
            ? new NarrationBlock(block.Type, block.ModusMentis, cleanedText, block.Keywords, block.Actions, block.ChainOrigin,
                block.SourceObservationType, block.LinkedOutcome, block.KeywordOutcomeMap, block.Sentences, block.SpeakerName,
                block.OutcomeReports, block.DialogueOptions)
            : block;

        lock (_gate)
        {
            _blocks.Add(blockToAdd);
            RegenerateRenderedLines();
        }

        // Don't auto-scroll - let user see from the top
        // User can scroll down if needed
    }

    /// <summary>
    /// Get the visible window of lines, starting at the buffer's current <see cref="ScrollOffset"/>.
    /// The buffer owns the scroll position — callers move it with
    /// <see cref="ScrollUp"/>/<see cref="ScrollDown"/>/<see cref="SetScrollOffset"/>/<see cref="ScrollToBottom"/>.
    /// This matters because the narration and dialogue panels share one buffer and must therefore
    /// share one scroll position.
    /// </summary>
    public List<RenderedLine> GetVisibleLines(int visibleCount)
    {
        lock (_gate)
        {
            if (_renderedLines.Count == 0)
                return new List<RenderedLine>();

            int actualStart = Math.Max(0, Math.Min(_scrollOffset, _renderedLines.Count - 1));
            int actualCount = Math.Min(visibleCount, _renderedLines.Count - actualStart);

            if (actualCount <= 0)
                return new List<RenderedLine>();

            return _renderedLines.Skip(actualStart).Take(actualCount).ToList();
        }
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
        int maxScroll = MaxScrollOffset();
        if (_scrollOffset >= maxScroll)
            return;

        _scrollOffset = Math.Min(maxScroll, _scrollOffset + lines);
    }

    /// <summary>
    /// Scroll to the bottom of the buffer.
    /// </summary>
    public void ScrollToBottom()
    {
        _scrollOffset = MaxScrollOffset();
    }

    /// <summary>
    /// Set the scroll offset directly (for scrollbar dragging).
    /// </summary>
    public void SetScrollOffset(int offset)
    {
        _scrollOffset = Math.Clamp(offset, 0, MaxScrollOffset());
    }

    private int MaxScrollOffset()
    {
        lock (_gate)
            return Math.Max(0, _layout.CalculateMaxScrollOffset(_renderedLines.Count));
    }

    /// <summary>
    /// Can we scroll up?
    /// </summary>
    public bool CanScrollUp() => _scrollOffset > 0;

    /// <summary>
    /// Can we scroll down?
    /// </summary>
    public bool CanScrollDown(int visibleLines)
    {
        lock (_gate)
            return _scrollOffset + visibleLines < _renderedLines.Count;
    }

    /// <summary>
    /// Clear all blocks and rendered lines (including history).
    /// </summary>
    public void Clear()
    {
        lock (_gate)
        {
            _blocks.Clear();
            _renderedLines.Clear();
            _historyLines.Clear();
            _scrollOffset = 0;
        }
    }

    /// <summary>
    /// Convert the current live content to history (grayed out, non-interactive) and close it with a
    /// separator rule. This preserves the text for player reference while a new segment — a new
    /// narration node, or a whole new phase such as a conversation or a fight — begins.
    /// </summary>
    /// <param name="label">
    /// Optional caption centred in the separator naming what comes next (e.g. "conversation with Emma"),
    /// so long scroll-back history stays navigable. Null draws a plain rule.
    /// </param>
    public void ConvertToHistory(string? label = null)
    {
        lock (_gate)
        {
            int convertedCount = 0;

            // Only convert lines that are NOT already history
            // (_renderedLines contains both history lines at the start and current content after)
            foreach (var line in _renderedLines)
            {
                // Skip lines that are already in history
                if (line.IsHistory)
                    continue;

                // VerbAction lines are drawn live from their ParsedNarrativeAction: RenderActionLine
                // paints the "⑤ [MODUS MENTIS ⟐⟐] " prefix piece by piece and the buffer stores only
                // the wrapped text. History lines drop the action reference, so the prefix has to be
                // baked into the text here — otherwise the header vanishes when the segment greys
                // out. GlobalActionIndex separates real action lines from Action-block prose.
                string historyText = line.Text;
                if (line.Type == LineType.Action && line.GlobalActionIndex >= 0)
                    historyText = line.Actions is { Count: > 0 }
                        ? line.Actions[0].DisplayPrefix + historyText
                        : "    " + historyText;      // continuation line: the live 4-space indent

                // Create new line with IsHistory=true, and clear interactive elements
                var historyLine = new RenderedLine(
                    Text: historyText,
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

            // The separator is emitted even when nothing was converted: a phase entered on an empty
            // buffer still needs its caption, otherwise the segment silently loses its heading.
            _historyLines.Add(BuildSeparator(label));

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

            Console.WriteLine($"ConvertToHistory({label ?? "-"}): converted {convertedCount} lines; " +
                              $"{_historyLines.Count} history, {_renderedLines.Count} total");
        }

        // Park at the tail so the new segment's content appears at the bottom. Uses the shared
        // clamp so SCROLL_BOTTOM_MARGIN is respected exactly like every other scroll path.
        ScrollToBottom();
    }

    /// <summary>
    /// Build the separator rule closing a segment: a plain dashed line, or the label centred in it
    /// with at least four dashes on each side.
    /// </summary>
    private RenderedLine BuildSeparator(string? label)
    {
        string text;
        if (string.IsNullOrWhiteSpace(label))
        {
            text = new string('─', Math.Min(_maxWidth, 40));
        }
        else
        {
            const int MinDashesPerSide = 4;
            int maxLabel = _maxWidth - 2 * (MinDashesPerSide + 1); // room for dashes + the flanking spaces
            string caption = label.Trim();
            if (maxLabel > 0 && caption.Length > maxLabel) caption = caption[..maxLabel];

            string padded = $" {caption} ";
            int dashes = Math.Max(2 * MinDashesPerSide, _maxWidth - padded.Length);
            int left   = dashes / 2;
            text = new string('─', left) + padded + new string('─', dashes - left);
        }

        return new RenderedLine(
            Text: text,
            Type: LineType.Separator,
            BlockType: NarrationBlockType.Observation, // Doesn't matter for separator
            Keywords: null,
            Actions: null,
            IsHistory: true,
            GlobalActionIndex: -1,
            SourceBlock: null
        );
    }

    /// <summary>
    /// Regenerate all rendered lines from blocks with word wrapping.
    /// History lines are prepended at the top.
    /// Callers must hold <see cref="_gate"/>.
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
                string headerText = block.Type is NarrationBlockType.Speaking or NarrationBlockType.PlayerSpeaking
                                    && block.SpeakerName != null
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
                NarrationBlockType.PlayerSpeaking => LineType.Content, // colour comes from BlockType
                _ => LineType.Content
            };

            // Dialogue options: no prose to wrap — render each selectable reply like a
            // thinking-block action line ("> [Skill ▪▪] “text”", continuations indented 4) and tag
            // every wrapped line with its option index so the dialogue panel can hit-test and
            // restyle it (hover / selected / greyed) without regenerating the buffer.
            if (block.Type == NarrationBlockType.DialogueOptions)
            {
                for (int i = 0; block.DialogueOptions != null && i < block.DialogueOptions.Count; i++)
                {
                    var opt = block.DialogueOptions[i];
                    string levelDots = new string(Config.Symbols.ModusMentisLevelIndicator, opt.SkillLevel);
                    string optPrefix = $"> [{opt.SkillName} {levelDots}] ";
                    int firstLineWidth    = Math.Max(4, _maxWidth - optPrefix.Length);
                    int continuationWidth = Math.Max(4, _maxWidth - 4);
                    var wrapped = WrapActionText($"“{opt.Text}”", firstLineWidth, continuationWidth);
                    for (int ln = 0; ln < wrapped.Count; ln++)
                    {
                        _renderedLines.Add(new RenderedLine(
                            Text: ln == 0 ? optPrefix + wrapped[ln] : "    " + wrapped[ln],
                            Type: LineType.DialogueOption,
                            BlockType: block.Type,
                            Keywords: null,
                            Actions: null,
                            IsHistory: false,
                            GlobalActionIndex: -1,
                            SourceBlock: block,
                            DialogueOptionIndex: i));
                    }
                }
            }
            // Wrap block.Text as one continuous paragraph (preserving natural word-flow).
            // When per-sentence data is available, compute which character range each sentence
            // occupies in block.Text and assign each wrapped line only the keywords from the
            // sentence(s) that overlap its character range — preventing cross-sentence highlighting.
            else if (block.Sentences != null && block.Sentences.Count > 0)
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

            // Outcome report chips — one line per report, preceded by an empty separator line.
            if (block.OutcomeReports != null && block.OutcomeReports.Count > 0)
            {
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

                foreach (var report in block.OutcomeReports.Where(r => r.ShowInUI))
                {
                    // Pad text to fill content width so the background chip spans the full line.
                    int leftPad  = Math.Max(0, (_maxWidth - report.Text.Length) / 2);
                    int rightPad = Math.Max(0, _maxWidth - report.Text.Length - leftPad);
                    string chipText = new string(' ', leftPad) + report.Text + new string(' ', rightPad);
                    _renderedLines.Add(new RenderedLine(
                        Text: chipText,
                        Type: LineType.Report,
                        BlockType: block.Type,
                        Keywords: null,
                        Actions: null,
                        IsHistory: false,
                        GlobalActionIndex: -1,
                        SourceBlock: block,
                        KeywordOccurrenceIndices: null,
                        Report: report
                    ));
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
                // Match on a WHOLE WORD, exactly as the renderer's \b…\b highlighting does. A plain
                // substring search would land on the keyword hidden inside a larger word (e.g. "man"
                // inside "commanding"), and the occurrence index derived from it then fails to line up
                // with the renderer's word-boundary matches, so nothing gets highlighted.
                int kwPos = FirstWholeWord(blockText, kw, idx, sentEnd);
                if (kwPos >= 0)
                    kwsWithOffsets.Add((kw, kwPos));
            }
            ranges.Add((idx, sentEnd, kwsWithOffsets));
            searchFrom = sentEnd;
        }
        return ranges;
    }

    /// <summary>
    /// Counts how many whole-word occurrences of <paramref name="keyword"/> appear (case-insensitive)
    /// in <paramref name="text"/> strictly before <paramref name="upToIndex"/>. Matches on word
    /// boundaries (like the renderer) so the occurrence index it produces lines up with the
    /// renderer's <c>\b…\b</c> matches — a substring count would miscount keywords buried inside
    /// longer words (e.g. "man" in "commanding") and mis-target the highlight.
    /// </summary>
    private static int CountOccurrencesUpTo(string text, string keyword, int upToIndex)
    {
        if (string.IsNullOrEmpty(keyword)) return 0;
        int count = 0;
        foreach (Match m in Regex.Matches(text, WholeWordPattern(keyword), RegexOptions.IgnoreCase))
        {
            if (m.Index >= upToIndex) break;
            count++;
        }
        return count;
    }

    /// <summary>
    /// Returns the index of the first whole-word (word-boundary) occurrence of
    /// <paramref name="keyword"/> in <paramref name="text"/> within [<paramref name="start"/>,
    /// <paramref name="end"/>), or -1 if none. Iterates over full-text matches and filters by range
    /// so word boundaries are evaluated against the real neighbouring characters.
    /// </summary>
    private static int FirstWholeWord(string text, string keyword, int start, int end)
    {
        if (string.IsNullOrEmpty(keyword)) return -1;
        foreach (Match m in Regex.Matches(text, WholeWordPattern(keyword), RegexOptions.IgnoreCase))
        {
            if (m.Index >= start && m.Index < end) return m.Index;
            if (m.Index >= end) break;
        }
        return -1;
    }

    private static string WholeWordPattern(string keyword) => @"\b" + Regex.Escape(keyword) + @"\b";

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
    public IReadOnlyList<NarrationBlock> GetBlocks()
    {
        lock (_gate) return _blocks.ToList();
    }
}

/// <summary>
/// A single rendered line with metadata.
/// </summary>
public record RenderedLine(
    string Text,
    LineType Type,
    NarrationBlockType BlockType,
    List<string>? Keywords,
    List<ParsedNarrativeAction>? Actions,  // Actions for rendering (only for VerbAction lines)
    bool IsHistory = false,  // True if this line is part of history (from previous narration nodes)
    int GlobalActionIndex = -1,  // Global action index (0-based) across all thinking blocks, -1 if not an action line
    NarrationBlock? SourceBlock = null,  // The narration block this line comes from (for modusMentis chain tracking)
    List<int>? KeywordOccurrenceIndices = null,  // Parallel to Keywords: which occurrence (0-based) within this line to highlight
    Outcome? Report = null,  // Set only for LineType.Report lines; null for all other types
    int DialogueOptionIndex = -1  // 0-based index into SourceBlock.DialogueOptions for LineType.DialogueOption lines, -1 otherwise
);

/// <summary>
/// Type of rendered line.
/// </summary>
public enum LineType
{
    Header,         // ModusMentis name header
    Content,        // Narration text
    Action,         // Action line (for Thinking blocks)
    Outcome,        // Outcome narration (for Action/Outcome blocks)
    Report,         // Prewritten outcome chip (item received, wound, …)
    Empty,          // Spacing
    Separator,      // Transition separator between narration nodes
    DialogueOption  // Selectable player reply line (for DialogueOptions blocks)
}
