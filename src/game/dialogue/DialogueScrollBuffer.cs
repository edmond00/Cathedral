using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Dialogue;

// ── Block types ──────────────────────────────────────────────────────────────

public enum DialogueBlockType
{
    NpcSpeaking,    // NPC dialogue text
    PlayerReplica,  // Player's quoted replica
    SystemMessage,  // Metadata (affinity delta, separators, …)
    DiceRoll,       // Dice roll result line
}

/// <summary>A single unit of dialogue content added to the scroll buffer.</summary>
public record DialogueBlock(DialogueBlockType Type, string? Speaker, string Text);

// ── Rendered line ─────────────────────────────────────────────────────────────

public record DialogueRenderedLine(
    string Text,
    DialogueBlockType BlockType,
    int ReplicaIndex = -1   // ≥0 for player-selectable replica lines
);

// ── Buffer ────────────────────────────────────────────────────────────────────

/// <summary>
/// Scrollable buffer for dialogue blocks.
/// Simpler than NarrationScrollBuffer — no keyword regions, just word-wrapped lines.
/// </summary>
public class DialogueScrollBuffer
{
    private readonly List<DialogueBlock> _blocks = new();
    private readonly List<DialogueRenderedLine> _lines = new();
    private int _scrollOffset;
    private readonly int _maxWidth;

    public int ScrollOffset => _scrollOffset;
    public int TotalLines   => _lines.Count;

    public DialogueScrollBuffer(int maxWidth)
    {
        _maxWidth = Math.Max(20, maxWidth);
    }

    public void Clear()
    {
        _blocks.Clear();
        _lines.Clear();
        _scrollOffset = 0;
    }

    public void AddBlock(DialogueBlock block)
    {
        _blocks.Add(block);
        AppendRenderedLines(block, _blocks.Count - 1);
    }

    /// <summary>Replace the last block (used to swap loading placeholder for real content).</summary>
    public void ReplaceLastBlock(DialogueBlock newBlock)
    {
        if (_blocks.Count == 0) { AddBlock(newBlock); return; }
        _blocks[^1] = newBlock;
        RegenerateLines();
    }

    public void AddSeparator()
    {
        AddBlock(new DialogueBlock(DialogueBlockType.SystemMessage, null, new string('─', Math.Min(_maxWidth, 40))));
        AddBlock(new DialogueBlock(DialogueBlockType.SystemMessage, null, ""));
    }

    public List<DialogueRenderedLine> GetVisibleLines(int count)
    {
        int actualStart = Math.Max(0, Math.Min(_scrollOffset, _lines.Count - 1));
        int actualCount = Math.Min(count, _lines.Count - actualStart);
        if (actualCount <= 0) return new List<DialogueRenderedLine>();
        return _lines.Skip(actualStart).Take(actualCount).ToList();
    }

    public void ScrollUp(int lines = 3)   => _scrollOffset = Math.Max(0, _scrollOffset - lines);
    public void ScrollDown(int lines = 3) => _scrollOffset = Math.Min(Math.Max(0, _lines.Count - 1), _scrollOffset + lines);
    public void ScrollToBottom()          => _scrollOffset = Math.Max(0, _lines.Count - 1);
    public bool CanScrollUp()   => _scrollOffset > 0;
    public bool CanScrollDown(int visible) => _scrollOffset + visible < _lines.Count;

    // ── Private rendering ────────────────────────────────────────────────────

    private void RegenerateLines()
    {
        _lines.Clear();
        int replicaCounter = 0;
        foreach (var block in _blocks)
        {
            AppendRenderedLinesInternal(block, ref replicaCounter);
        }
    }

    private void AppendRenderedLines(DialogueBlock block, int blockIndex)
    {
        // For full tracking, regenerate when needed — for performance, append incrementally.
        // Since replica indices are global, we must regenerate on each add to keep them consistent.
        RegenerateLines();
    }

    private void AppendRenderedLinesInternal(DialogueBlock block, ref int replicaCounter)
    {
        // Speaker header
        if (block.Speaker != null)
        {
            _lines.Add(new DialogueRenderedLine($"[ {block.Speaker} ]", block.Type));
        }

        // Word-wrap the text
        var wrapped = WordWrap(block.Text, _maxWidth - 2);
        bool isReplica = block.Type == DialogueBlockType.PlayerReplica;

        foreach (var (line, idx) in wrapped.Select((l, i) => (l, i)))
        {
            int replicaIdx = -1;
            if (isReplica && idx == 0)
            {
                replicaIdx = replicaCounter++;
            }
            _lines.Add(new DialogueRenderedLine($"  {line}", block.Type, replicaIdx));
        }

        // Trailing empty line after each block
        _lines.Add(new DialogueRenderedLine("", block.Type));
    }

    private static List<string> WordWrap(string text, int maxWidth)
    {
        if (maxWidth <= 0) return new List<string> { text };

        var result = new List<string>();
        var words = text.Split(' ');
        var currentLine = string.Empty;

        foreach (var word in words)
        {
            if (currentLine.Length == 0)
            {
                currentLine = word;
            }
            else if (currentLine.Length + 1 + word.Length <= maxWidth)
            {
                currentLine += " " + word;
            }
            else
            {
                result.Add(currentLine);
                currentLine = word;
            }
        }

        if (currentLine.Length > 0)
            result.Add(currentLine);

        if (result.Count == 0)
            result.Add("");

        return result;
    }
}
