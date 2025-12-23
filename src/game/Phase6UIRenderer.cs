using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// Terminal UI renderer for Phase 6 Chain-of-Thought narrative system
/// Handles scrollable narration blocks, clickable keywords, thinking attempts indicator
/// </summary>
public class Phase6UIRenderer
{
    private readonly TerminalHUD _terminal;
    private readonly NarrationScrollBuffer _scrollBuffer;
    
    // Layout constants (100x30 grid)
    private const int HeaderLines = 2;
    private const int NarrationStartY = 2;
    private const int NarrationEndY = 28;
    private const int StatusBarY = 29;
    private const int NarrationVisibleLines = NarrationEndY - NarrationStartY;
    private const int TerminalWidth = 100;

    // Color scheme
    private readonly Vector4 HeaderColor = new(0.0f, 0.8f, 1.0f, 1.0f);        // Cyan
    private readonly Vector4 SkillHeaderColor = new(1.0f, 1.0f, 0.0f, 1.0f);   // Yellow
    private readonly Vector4 ObservationColor = new(0.7f, 0.7f, 0.7f, 1.0f);   // Light gray
    private readonly Vector4 ThinkingColor = new(1.0f, 0.9f, 0.5f, 1.0f);      // Yellow-ish
    private readonly Vector4 KeywordNormalColor = new(0.5f, 0.9f, 1.0f, 1.0f); // Light cyan
    private readonly Vector4 KeywordHoverColor = new(0.2f, 1.0f, 1.0f, 1.0f);  // Bright cyan
    private readonly Vector4 KeywordDisabledColor = new(0.3f, 0.3f, 0.3f, 1.0f); // Dark gray
    private readonly Vector4 ActionNormalColor = new(1.0f, 1.0f, 1.0f, 1.0f);  // White
    private readonly Vector4 ActionHoverColor = new(1.0f, 1.0f, 0.0f, 1.0f);   // Yellow
    private readonly Vector4 ActionSkillColor = new(0.0f, 1.0f, 0.0f, 1.0f);   // Green
    private readonly Vector4 StatusBarColor = new(0.5f, 0.5f, 0.5f, 1.0f);     // Gray
    private readonly Vector4 ThinkingAttemptsColor = new(0.8f, 0.4f, 0.4f, 1.0f); // Red-ish

    // Interactive regions
    private readonly List<KeywordRegion> _keywordRegions = new();
    private readonly List<ActionRegion> _actionRegions = new();
    private bool _keywordsEnabled = false;
    private string? _hoveredKeyword = null;
    private int? _hoveredActionIndex = null;

    // Loading animation state
    private bool _isLoading = false;
    private string _loadingMessage = "";
    private readonly string[] _loadingFrames = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
    private int _loadingFrameIndex = 0;

    public Phase6UIRenderer(TerminalHUD terminal, int visibleLines = NarrationVisibleLines)
    {
        _terminal = terminal;
        _scrollBuffer = new NarrationScrollBuffer(visibleLines, TerminalWidth - 2);
    }

    public NarrationScrollBuffer ScrollBuffer => _scrollBuffer;
    public bool KeywordsEnabled => _keywordsEnabled;

    /// <summary>
    /// Clear the entire UI
    /// </summary>
    public void Clear()
    {
        _terminal.Clear();
        _scrollBuffer.Clear();
        _keywordRegions.Clear();
        _actionRegions.Clear();
        _hoveredKeyword = null;
        _hoveredActionIndex = null;
        _keywordsEnabled = false;
    }

    /// <summary>
    /// Render the complete UI
    /// </summary>
    public void Render(string nodeName, int thinkingAttemptsRemaining, int maxThinkingAttempts = 3)
    {
        _terminal.Clear();

        if (_isLoading)
        {
            RenderLoading();
            return;
        }

        // Header
        RenderHeader(nodeName, thinkingAttemptsRemaining, maxThinkingAttempts);

        // Narration blocks (scrollable)
        RenderNarrationBlocks();

        // Status bar
        RenderStatusBar(thinkingAttemptsRemaining);
    }

    /// <summary>
    /// Enable or disable keyword interaction
    /// </summary>
    public void SetKeywordsEnabled(bool enabled)
    {
        _keywordsEnabled = enabled;
    }

    /// <summary>
    /// Show loading indicator with message
    /// </summary>
    public void ShowLoadingIndicator(string message)
    {
        _isLoading = true;
        _loadingMessage = message;
        _loadingFrameIndex = (_loadingFrameIndex + 1) % _loadingFrames.Length;
    }

    /// <summary>
    /// Hide loading indicator
    /// </summary>
    public void HideLoadingIndicator()
    {
        _isLoading = false;
    }

    /// <summary>
    /// Update hover state based on mouse position
    /// </summary>
    public void UpdateHover(int mouseX, int mouseY)
    {
        // Check keyword hover
        string? newHoveredKeyword = null;
        if (_keywordsEnabled)
        {
            foreach (var region in _keywordRegions)
            {
                if (mouseY == region.Y && mouseX >= region.StartX && mouseX < region.EndX)
                {
                    newHoveredKeyword = region.Keyword;
                    break;
                }
            }
        }

        // Check action hover
        int? newHoveredAction = null;
        foreach (var region in _actionRegions)
        {
            if (mouseY >= region.StartY && mouseY <= region.EndY &&
                mouseX >= region.StartX && mouseX < region.EndX)
            {
                newHoveredAction = region.ActionIndex;
                break;
            }
        }

        // Re-render if hover state changed
        if (newHoveredKeyword != _hoveredKeyword || newHoveredAction != _hoveredActionIndex)
        {
            _hoveredKeyword = newHoveredKeyword;
            _hoveredActionIndex = newHoveredAction;
            // Caller should call Render() after this
        }
    }

    /// <summary>
    /// Get keyword at mouse position
    /// </summary>
    public string? GetClickedKeyword(int mouseX, int mouseY)
    {
        if (!_keywordsEnabled) return null;

        foreach (var region in _keywordRegions)
        {
            if (mouseY == region.Y && mouseX >= region.StartX && mouseX < region.EndX)
            {
                return region.Keyword;
            }
        }
        return null;
    }

    /// <summary>
    /// Get action at mouse position
    /// </summary>
    public int? GetClickedActionIndex(int mouseX, int mouseY)
    {
        foreach (var region in _actionRegions)
        {
            if (mouseY >= region.StartY && mouseY <= region.EndY &&
                mouseX >= region.StartX && mouseX < region.EndX)
            {
                return region.ActionIndex;
            }
        }
        return null;
    }

    /// <summary>
    /// Scroll up
    /// </summary>
    public void ScrollUp(int lines = 1)
    {
        _scrollBuffer.ScrollUp(lines);
    }

    /// <summary>
    /// Scroll down
    /// </summary>
    public void ScrollDown(int lines = 1)
    {
        _scrollBuffer.ScrollDown(lines);
    }

    private void RenderHeader(string nodeName, int attemptsRemaining, int maxAttempts)
    {
        // Line 0: Node name + thinking attempts
        _terminal.Text(0, 0, $"Forest Exploration - {nodeName}", HeaderColor, new Vector4(0, 0, 0, 1));

        // Right-aligned thinking attempts indicator
        string attemptsText = "Thinking: ";
        int xStart = TerminalWidth - attemptsText.Length - (maxAttempts * 5) - 1;
        _terminal.Text(xStart, 0, attemptsText, HeaderColor, new Vector4(0, 0, 0, 1));
        
        int xOffset = xStart + attemptsText.Length;
        for (int i = 0; i < maxAttempts; i++)
        {
            string block = i < attemptsRemaining ? "[██]" : "[  ]";
            var color = i < attemptsRemaining ? ThinkingAttemptsColor : new Vector4(0.3f, 0.3f, 0.3f, 1.0f);
            _terminal.Text(xOffset, 0, block, color, new Vector4(0, 0, 0, 1));
            xOffset += 5;
        }

        // Line 1: Separator
        _terminal.Text(0, 1, new string('─', TerminalWidth), HeaderColor, new Vector4(0, 0, 0, 1));
    }

    private void RenderNarrationBlocks()
    {
        _keywordRegions.Clear();
        _actionRegions.Clear();

        var visibleLines = _scrollBuffer.GetVisibleLines();
        int currentY = NarrationStartY;

        foreach (var line in visibleLines)
        {
            if (currentY >= NarrationEndY) break;

            switch (line.Type)
            {
                case LineType.SkillHeader:
                    _terminal.Text(1, currentY, line.Text, SkillHeaderColor, new Vector4(0, 0, 0, 1));
                    break;

                case LineType.ObservationText:
                    RenderTextWithKeywords(line.Text, 1, currentY, ObservationColor, line.SourceBlock.Keywords);
                    break;

                case LineType.ThinkingText:
                    _terminal.Text(1, currentY, line.Text, ThinkingColor, new Vector4(0, 0, 0, 1));
                    break;

                case LineType.ActionItem:
                    if (line.Action != null)
                    {
                        RenderActionItem(line.Text, 1, currentY, line.Action, _actionRegions.Count);
                    }
                    break;

                case LineType.OutcomeText:
                    _terminal.Text(1, currentY, line.Text, ObservationColor, new Vector4(0, 0, 0, 1));
                    break;

                case LineType.NarrativeText:
                    _terminal.Text(1, currentY, line.Text, ObservationColor, new Vector4(0, 0, 0, 1));
                    break;
            }

            currentY++;
        }
    }

    private void RenderTextWithKeywords(string text, int x, int y, Vector4 baseColor, List<string>? keywords)
    {
        if (keywords == null || keywords.Count == 0)
        {
            _terminal.Text(x, y, text, baseColor, new Vector4(0, 0, 0, 1));
            return;
        }

        // Find all keyword positions in text
        var keywordPositions = new List<(int start, int length, string keyword)>();
        foreach (var keyword in keywords)
        {
            int index = 0;
            while ((index = text.IndexOf(keyword, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                keywordPositions.Add((index, keyword.Length, keyword));
                index += keyword.Length;
            }
        }

        // Sort by position
        keywordPositions = keywordPositions.OrderBy(k => k.start).ToList();

        // Render text with keywords highlighted
        int currentPos = 0;
        foreach (var (start, length, keyword) in keywordPositions)
        {
            // Render text before keyword
            if (start > currentPos)
            {
                string beforeText = text.Substring(currentPos, start - currentPos);
                _terminal.Text(x + currentPos, y, beforeText, baseColor, new Vector4(0, 0, 0, 1));
            }

            // Render keyword with highlighting
            string keywordText = text.Substring(start, length);
            var keywordColor = _keywordsEnabled
                ? (_hoveredKeyword == keyword ? KeywordHoverColor : KeywordNormalColor)
                : KeywordDisabledColor;
            
            _terminal.Text(x + start, y, keywordText, keywordColor, new Vector4(0, 0, 0, 1));

            // Track keyword region for click detection
            if (_keywordsEnabled)
            {
                _keywordRegions.Add(new KeywordRegion(keyword, y, x + start, x + start + length));
            }

            currentPos = start + length;
        }

        // Render remaining text
        if (currentPos < text.Length)
        {
            string remainingText = text.Substring(currentPos);
            _terminal.Text(x + currentPos, y, remainingText, baseColor, new Vector4(0, 0, 0, 1));
        }
    }

    private void RenderActionItem(string text, int x, int y, ParsedNarrativeAction action, int actionIndex)
    {
        bool isHovered = _hoveredActionIndex == actionIndex;
        var color = isHovered ? ActionHoverColor : ActionNormalColor;

        // Parse action line: "  > [SkillName] description"
        int skillBracketStart = text.IndexOf('[');
        int skillBracketEnd = text.IndexOf(']');

        if (skillBracketStart >= 0 && skillBracketEnd > skillBracketStart)
        {
            // Render "  > " prefix
            string prefix = text.Substring(0, skillBracketStart);
            _terminal.Text(x, y, prefix, new Vector4(0.5f, 0.5f, 0.5f, 1.0f), new Vector4(0, 0, 0, 1));

            // Render [SkillName]
            string skillPart = text.Substring(skillBracketStart, skillBracketEnd - skillBracketStart + 1);
            _terminal.Text(x + prefix.Length, y, skillPart, ActionSkillColor, new Vector4(0, 0, 0, 1));

            // Render description
            if (skillBracketEnd + 1 < text.Length)
            {
                string description = text.Substring(skillBracketEnd + 1);
                _terminal.Text(x + prefix.Length + skillPart.Length, y, description, color, new Vector4(0, 0, 0, 1));
            }
        }
        else
        {
            _terminal.Text(x, y, text, color, new Vector4(0, 0, 0, 1));
        }

        // Track action region for click detection
        _actionRegions.Add(new ActionRegion(actionIndex, y, y, x, x + text.Length));
    }

    private void RenderStatusBar(int attemptsRemaining)
    {
        string message = attemptsRemaining > 0
            ? $"Hover keywords to highlight • Click keywords to think ({attemptsRemaining} attempts remaining)"
            : "No thinking attempts remaining • Select an action to proceed";

        // Center the message
        int xStart = (TerminalWidth - message.Length) / 2;
        _terminal.Text(xStart, StatusBarY, message, StatusBarColor, new Vector4(0, 0, 0, 1));
    }

    private void RenderLoading()
    {
        int centerY = 15;
        
        // Progress bar
        string progressBar = "[█▓▒░░░░░░░░░░░░░░░░░░░░░░░░░█▓▒]";
        int barX = (TerminalWidth - progressBar.Length) / 2;
        _terminal.Text(barX, centerY - 2, progressBar, HeaderColor, new Vector4(0, 0, 0, 1));

        // Spinner + message
        string spinner = _loadingFrames[_loadingFrameIndex];
        string message = $"  {spinner}  {_loadingMessage}  {spinner}  ";
        int msgX = (TerminalWidth - message.Length) / 2;
        _terminal.Text(msgX, centerY, message, new Vector4(1.0f, 1.0f, 1.0f, 1.0f), new Vector4(0, 0, 0, 1));

        // "Please wait..."
        string wait = "Please wait...";
        int waitX = (TerminalWidth - wait.Length) / 2;
        _terminal.Text(waitX, centerY + 2, wait, StatusBarColor, new Vector4(0, 0, 0, 1));
    }
}

/// <summary>
/// Clickable keyword region
/// </summary>
public record KeywordRegion(string Keyword, int Y, int StartX, int EndX);

/// <summary>
/// Clickable action region
/// </summary>
public record ActionRegion(int ActionIndex, int StartY, int EndY, int StartX, int EndX);
