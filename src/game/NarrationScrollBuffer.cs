using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// Manages scrollable narration history with viewport rendering
/// Tracks all NarrationBlock instances and renders only visible lines
/// </summary>
public class NarrationScrollBuffer
{
    private readonly List<NarrationBlock> _blocks = new();
    private int _scrollOffset = 0;
    private readonly int _visibleLines;
    private readonly int _maxWidth;
    
    // Cached rendered lines for performance
    private List<RenderedLine> _cachedLines = new();
    private bool _isDirty = true;

    public NarrationScrollBuffer(int visibleLines, int maxWidth)
    {
        _visibleLines = visibleLines;
        _maxWidth = maxWidth;
    }

    public int BlockCount => _blocks.Count;
    public int ScrollOffset => _scrollOffset;
    public int TotalLines => _cachedLines.Count;
    public bool CanScrollUp => _scrollOffset > 0;
    public bool CanScrollDown => _scrollOffset + _visibleLines < _cachedLines.Count;

    /// <summary>
    /// Add a new narration block to the history
    /// </summary>
    public void AddBlock(NarrationBlock block)
    {
        _blocks.Add(block);
        _isDirty = true;
    }

    /// <summary>
    /// Clear all blocks and reset scroll
    /// </summary>
    public void Clear()
    {
        _blocks.Clear();
        _cachedLines.Clear();
        _scrollOffset = 0;
        _isDirty = true;
    }

    /// <summary>
    /// Scroll up by specified number of lines
    /// </summary>
    public void ScrollUp(int lines = 1)
    {
        _scrollOffset = Math.Max(0, _scrollOffset - lines);
    }

    /// <summary>
    /// Scroll down by specified number of lines
    /// </summary>
    public void ScrollDown(int lines = 1)
    {
        int maxOffset = Math.Max(0, _cachedLines.Count - _visibleLines);
        _scrollOffset = Math.Min(maxOffset, _scrollOffset + lines);
    }

    /// <summary>
    /// Scroll to bottom (show most recent content)
    /// </summary>
    public void ScrollToBottom()
    {
        RebuildCacheIfNeeded();
        _scrollOffset = Math.Max(0, _cachedLines.Count - _visibleLines);
    }

    /// <summary>
    /// Scroll to top
    /// </summary>
    public void ScrollToTop()
    {
        _scrollOffset = 0;
    }

    /// <summary>
    /// Get visible lines for rendering (from current viewport)
    /// </summary>
    public List<RenderedLine> GetVisibleLines()
    {
        RebuildCacheIfNeeded();
        
        if (_cachedLines.Count == 0)
            return new List<RenderedLine>();

        int endIndex = Math.Min(_scrollOffset + _visibleLines, _cachedLines.Count);
        return _cachedLines.GetRange(_scrollOffset, endIndex - _scrollOffset);
    }

    /// <summary>
    /// Get all blocks (for keyword extraction, etc.)
    /// </summary>
    public IReadOnlyList<NarrationBlock> GetAllBlocks() => _blocks.AsReadOnly();

    /// <summary>
    /// Get all keywords from observation blocks
    /// </summary>
    public List<string> GetAllKeywords()
    {
        var keywords = new List<string>();
        foreach (var block in _blocks)
        {
            if (block.Type == NarrationBlockType.Observation && block.Keywords != null)
            {
                keywords.AddRange(block.Keywords);
            }
        }
        return keywords.Distinct().ToList();
    }

    /// <summary>
    /// Rebuild cached lines from blocks (word wrap + formatting)
    /// </summary>
    private void RebuildCacheIfNeeded()
    {
        if (!_isDirty) return;

        _cachedLines.Clear();

        foreach (var block in _blocks)
        {
            // Add skill header
            string header = $"[{block.SkillName.ToUpper()}]";
            _cachedLines.Add(new RenderedLine(header, LineType.SkillHeader, block));

            // Add content (word-wrapped)
            var wrappedLines = WrapText(block.Text, _maxWidth);
            foreach (var line in wrappedLines)
            {
                var lineType = block.Type switch
                {
                    NarrationBlockType.Observation => LineType.ObservationText,
                    NarrationBlockType.Thinking => LineType.ThinkingText,
                    NarrationBlockType.Action => LineType.ActionText,
                    NarrationBlockType.Outcome => LineType.OutcomeText,
                    _ => LineType.NarrativeText
                };
                _cachedLines.Add(new RenderedLine(line, lineType, block));
            }

            // Add actions if present
            if (block.Actions != null && block.Actions.Count > 0)
            {
                _cachedLines.Add(new RenderedLine("", LineType.NarrativeText, block)); // Blank line
                
                foreach (var action in block.Actions)
                {
                    // Format: > [Skill Name] action description (without "try to ")
                    string actionSkillName = action.ActionSkill?.DisplayName ?? "Unknown";
                    string description = action.DisplayText;
                    
                    // Remove "try to " prefix if present
                    if (description.StartsWith("try to ", StringComparison.OrdinalIgnoreCase))
                    {
                        description = description.Substring(7);
                    }
                    
                    string actionLine = $"  > [{actionSkillName}] {description}";
                    
                    // Word wrap if needed
                    var actionWrapped = WrapText(actionLine, _maxWidth);
                    foreach (var line in actionWrapped)
                    {
                        _cachedLines.Add(new RenderedLine(line, LineType.ActionItem, block, action));
                    }
                }
            }

            // Add blank line between blocks
            _cachedLines.Add(new RenderedLine("", LineType.NarrativeText, block));
        }

        _isDirty = false;
    }

    /// <summary>
    /// Word wrap text to fit width, preserving paragraphs
    /// </summary>
    private List<string> WrapText(string text, int maxWidth)
    {
        var lines = new List<string>();
        if (string.IsNullOrWhiteSpace(text))
        {
            lines.Add("");
            return lines;
        }

        string[] paragraphs = text.Split(new[] { '\n', '\r' }, StringSplitOptions.None);

        foreach (string paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                lines.Add("");
                continue;
            }

            string[] words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            StringBuilder currentLine = new StringBuilder();

            foreach (string word in words)
            {
                string testLine = currentLine.Length == 0 ? word : currentLine + " " + word;

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

                    // Handle very long words
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

/// <summary>
/// Represents a single rendered line with formatting metadata
/// </summary>
public record RenderedLine(
    string Text,
    LineType Type,
    NarrationBlock SourceBlock,
    ParsedNarrativeAction? Action = null
);

public enum LineType
{
    SkillHeader,        // [OBSERVATION]
    ObservationText,    // Gray observation text
    ThinkingText,       // Yellow thinking text
    ActionText,         // White action announcement
    OutcomeText,        // Outcome result text
    NarrativeText,      // Generic text
    ActionItem          // Clickable action item (> [Skill] description)
}


