using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// The scrollable text of the conversations held during a visit: blocks wrapped into lines, the
/// visible window of them, and the earlier conversations kept above as greyed history. The narration
/// itself keeps no text here — its texts are on the thought trees.
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

    /// <summary>
    /// Raised after a block is committed to the narration — the moment the player is shown it. The
    /// journal listens here (see <see cref="JournalRecorder"/>), which is what keeps unshown preview
    /// text out of it.
    /// </summary>
    public event Action<NarrationBlock>? BlockAdded;
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
            ? new NarrationBlock(block.Type, block.ModusMentis, cleanedText, block.Actions, block.ChainOrigin,
                block.LinkedOutcome, block.SpeakerName, block.OutcomeReports, block.DialogueOptions)
            : block;

        lock (_gate)
        {
            _blocks.Add(blockToAdd);
            RegenerateRenderedLines();
        }

        BlockAdded?.Invoke(blockToAdd);

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

                // Create new line with IsHistory=true, and clear interactive elements
                var historyLine = new RenderedLine(
                    Text: line.Text,
                    Type: line.Type,
                    BlockType: line.BlockType,
                    IsHistory: true,
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
                IsHistory: true,
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
    /// with at least four dashes on each side. Either way it spans the panel — an unlabelled rule
    /// used to stop at 40 cells, so the same event drew a full-width captioned rule or a half-width
    /// blank one depending only on whether a caption happened to be passed, which reads as two
    /// different kinds of break rather than one.
    /// </summary>
    private RenderedLine BuildSeparator(string? label)
    {
        string text;
        if (string.IsNullOrWhiteSpace(label))
        {
            text = new string('─', _maxWidth);
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
            IsHistory: true,
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
                    IsHistory: false,
                    SourceBlock: block
                ));
                
                // Empty line after header
                _renderedLines.Add(new RenderedLine(
                    Text: "",
                    Type: LineType.Empty,
                    BlockType: block.Type,
                    IsHistory: false,
                    SourceBlock: block
                ));
            }

            // Wrap and add narration content.
            // Determine line type based on block type
            LineType lineType = block.Type switch
            {
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
                            IsHistory: false,
                            SourceBlock: block,
                            DialogueOptionIndex: i));
                    }
                }
            }
            else
            {
                var wrappedLines = WrapText(block.Text, _maxWidth);
                foreach (var line in wrappedLines)
                {
                    _renderedLines.Add(new RenderedLine(
                        Text: line,
                        Type: lineType,
                        BlockType: block.Type,
                        IsHistory: false,
                        SourceBlock: block
                    ));
                }
            }

            // Outcome report chips — one line per report, preceded by an empty separator line.
            if (block.OutcomeReports != null && block.OutcomeReports.Count > 0)
            {
                _renderedLines.Add(new RenderedLine(
                    Text: "",
                    Type: LineType.Empty,
                    BlockType: block.Type,
                    IsHistory: false,
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
                        IsHistory: false,
                        SourceBlock: block,
                        Report: report
                    ));
                }
            }

            // Empty line after block
            _renderedLines.Add(new RenderedLine(
                Text: "",
                Type: LineType.Empty,
                BlockType: block.Type,
                IsHistory: false,
                SourceBlock: block
            ));
        }
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
    bool IsHistory = false,  // True if this line is part of history (an earlier conversation)
    NarrationBlock? SourceBlock = null,  // The block this line comes from
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
    Outcome,        // Outcome narration (for Action/Outcome blocks)
    Report,         // Prewritten outcome chip (item received, wound, …)
    Empty,          // Spacing
    Separator,      // Transition separator between narration nodes
    DialogueOption  // Selectable player reply line (for DialogueOptions blocks)
}
