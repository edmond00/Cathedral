using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Terminal;

namespace Cathedral.Game;

/// <summary>
/// Abstract base class for bordered terminal-panel UIs (NarrativeUI, DialogueUI, …).
/// Encapsulates the shared infrastructure that every panel needs:
///   • Padding-zone border rendering  (Clear / ClearContent)
///   • Horizontal separator lines     (DrawHorizontalLine)
///   • Proportional scrollbar         (RenderScrollbar + hit-test helpers)
///   • Animated loading spinner       (ShowLoadingIndicator  — public virtual)
///   • Centered error display         (ShowError             — public virtual)
///   • Status bar                     (DrawStatusBar         — protected)
///   • Word-wrap helper               (WrapText              — protected)
///
/// All layout is derived from <see cref="NarrativeLayout"/> using the values
/// declared in <see cref="Config.NarrativeUI"/>.
/// </summary>
public abstract class TerminalPanelUI
{
    // ── Shared fields (accessible to subclasses) ──────────────────────────────
    protected readonly TerminalHUD    _terminal;
    protected readonly NarrativeLayout _layout;
    protected readonly int             _scrollbarX;

    /// <summary>Current animation frame for the loading spinner (shared with dice-roll animation).</summary>
    protected int      _loadingFrameIndex;
    protected DateTime _lastFrameUpdate = DateTime.Now;

    // ── Constructor ──────────────────────────────────────────────────────────

    protected TerminalPanelUI(TerminalHUD terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _layout   = new NarrativeLayout(
            terminal.Width,
            terminal.Height,
            Config.NarrativeUI.TopPadding,
            Config.NarrativeUI.BottomPadding,
            Config.NarrativeUI.LeftPadding,
            Config.NarrativeUI.RightPadding);
        _scrollbarX = _layout.TERMINAL_WIDTH - _layout.RIGHT_PADDING - _layout.RIGHT_MARGIN;
    }

    // ── Clearing ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Clear the entire terminal.
    /// Padding zones receive the configured border characters/colours;
    /// the content zone is filled with the background colour.
    /// Override to also reset subclass-specific hit-tracking collections.
    /// </summary>
    public virtual void Clear()
    {
        for (int y = 0; y < _layout.TERMINAL_HEIGHT; y++)
        {
            for (int x = 0; x < _layout.TERMINAL_WIDTH; x++)
            {
                bool isTopPadding    = y <  _layout.TOP_PADDING;
                bool isBottomPadding = y >= _layout.TERMINAL_HEIGHT - _layout.BOTTOM_PADDING;
                bool isLeftPadding   = x <  _layout.LEFT_PADDING;
                bool isRightPadding  = x >= _layout.TERMINAL_WIDTH - _layout.RIGHT_PADDING;

                bool isTopEdge    = y == _layout.TOP_PADDING - 1;
                bool isBottomEdge = y == _layout.TERMINAL_HEIGHT - _layout.BOTTOM_PADDING;
                bool isLeftEdge   = x == _layout.LEFT_PADDING - 1;
                bool isRightEdge  = x == _layout.TERMINAL_WIDTH - _layout.RIGHT_PADDING;

                char    cellChar;
                Vector4 textColor, bgColor;

                if (isTopPadding)
                {
                    if (isTopEdge)
                    { cellChar = Config.NarrativeUI.TopPaddingEdgeChar; textColor = Config.NarrativeUI.TopPaddingEdgeTextColor; bgColor = Config.NarrativeUI.TopPaddingEdgeBackgroundColor; }
                    else
                    { cellChar = Config.NarrativeUI.TopPaddingChar; textColor = Config.NarrativeUI.TopPaddingTextColor; bgColor = Config.NarrativeUI.TopPaddingBackgroundColor; }
                }
                else if (isBottomPadding)
                {
                    if (isBottomEdge)
                    { cellChar = Config.NarrativeUI.BottomPaddingEdgeChar; textColor = Config.NarrativeUI.BottomPaddingEdgeTextColor; bgColor = Config.NarrativeUI.BottomPaddingEdgeBackgroundColor; }
                    else
                    { cellChar = Config.NarrativeUI.BottomPaddingChar; textColor = Config.NarrativeUI.BottomPaddingTextColor; bgColor = Config.NarrativeUI.BottomPaddingBackgroundColor; }
                }
                else if (isLeftPadding)
                {
                    if (isLeftEdge)
                    { cellChar = Config.NarrativeUI.LeftPaddingEdgeChar; textColor = Config.NarrativeUI.LeftPaddingEdgeTextColor; bgColor = Config.NarrativeUI.LeftPaddingEdgeBackgroundColor; }
                    else
                    { cellChar = Config.NarrativeUI.LeftPaddingChar; textColor = Config.NarrativeUI.LeftPaddingTextColor; bgColor = Config.NarrativeUI.LeftPaddingBackgroundColor; }
                }
                else if (isRightPadding)
                {
                    if (isRightEdge)
                    { cellChar = Config.NarrativeUI.RightPaddingEdgeChar; textColor = Config.NarrativeUI.RightPaddingEdgeTextColor; bgColor = Config.NarrativeUI.RightPaddingEdgeBackgroundColor; }
                    else
                    { cellChar = Config.NarrativeUI.RightPaddingChar; textColor = Config.NarrativeUI.RightPaddingTextColor; bgColor = Config.NarrativeUI.RightPaddingBackgroundColor; }
                }
                else
                {
                    cellChar = ' '; textColor = Config.NarrativeUI.NarrativeColor; bgColor = Config.NarrativeUI.BackgroundColor;
                }

                _terminal.SetCell(x, y, cellChar, textColor, bgColor);
            }
        }
    }

    /// <summary>Clear only the scrollable content area (between header and status-bar separator).</summary>
    protected void ClearContent()
    {
        for (int y = _layout.CONTENT_START_Y; y < _layout.SEPARATOR_Y + 1; y++)
            for (int x = _layout.LEFT_PADDING; x < _layout.TERMINAL_WIDTH - _layout.RIGHT_PADDING; x++)
                _terminal.SetCell(x, y, ' ', Config.NarrativeUI.NarrativeColor, Config.NarrativeUI.BackgroundColor);
    }

    // ── Separator lines ───────────────────────────────────────────────────────

    protected void DrawHorizontalLine(int y)
    {
        if (y < 0 || y >= _layout.TERMINAL_HEIGHT) return;
        for (int x = _layout.LEFT_PADDING; x < _layout.TERMINAL_WIDTH - _layout.RIGHT_PADDING; x++)
            _terminal.SetCell(x, y, Config.Symbols.HorizontalLine,
                Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);
    }

    // ── Scrollbar ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Draw a proportional scrollbar track + thumb.
    /// Returns the thumb's (StartY, Height) for subsequent hit-testing.
    /// </summary>
    protected (int StartY, int Height) RenderScrollbar(int totalLines, int scrollOffset, bool isThumbHovered)
    {
        int trackStartY = _layout.CONTENT_START_Y;
        int trackHeight = _layout.SCROLLBAR_TRACK_HEIGHT;

        for (int y = trackStartY; y < trackStartY + trackHeight; y++)
            _terminal.SetCell(_scrollbarX, y, '╏',
                Config.NarrativeUI.ScrollbarTrackColor, Config.NarrativeUI.BackgroundColor);

        int visibleLines = _layout.NARRATIVE_HEIGHT;
        if (totalLines <= visibleLines && scrollOffset == 0) return (0, 0);
        // Clamp totalLines so thumb ratio is valid when offset pushes content into view
        int effectiveTotal = Math.Max(totalLines, scrollOffset + visibleLines);

        float visibleRatio = (float)visibleLines / effectiveTotal;
        int   thumbHeight  = Math.Max(2, (int)(trackHeight * visibleRatio));
        int   maxScrollOff = _layout.CalculateMaxScrollOffset(effectiveTotal);
        float scrollRatio  = maxScrollOff > 0 ? (float)scrollOffset / maxScrollOff : 0f;
        int   thumbY       = trackStartY + (int)((trackHeight - thumbHeight) * scrollRatio);

        Vector4 thumbColor = isThumbHovered
            ? Config.NarrativeUI.ScrollbarThumbHoverColor
            : Config.NarrativeUI.ScrollbarThumbColor;

        for (int y = thumbY; y < thumbY + thumbHeight; y++)
            _terminal.SetCell(_scrollbarX, y, '█', thumbColor, Config.NarrativeUI.BackgroundColor);

        return (thumbY, thumbHeight);
    }

    protected bool IsMouseOverScrollbarThumb(int mouseX, int mouseY, (int StartY, int Height) thumb)
    {
        if (thumb.Height == 0) return false;
        return mouseX == _scrollbarX && mouseY >= thumb.StartY && mouseY < thumb.StartY + thumb.Height;
    }

    protected bool IsMouseOverScrollbarTrack(int mouseX, int mouseY, (int StartY, int Height) thumb)
    {
        if (mouseX != _scrollbarX) return false;
        int trackEndY = _layout.CONTENT_START_Y + _layout.SCROLLBAR_TRACK_HEIGHT;
        bool inTrack  = mouseY >= _layout.CONTENT_START_Y && mouseY < trackEndY;
        bool onThumb  = thumb.Height > 0 && mouseY >= thumb.StartY && mouseY < thumb.StartY + thumb.Height;
        return inTrack && !onThumb;
    }

    protected int CalculateScrollOffsetFromMouseY(int mouseY, int totalLines)
    {
        int trackHeight    = _layout.SCROLLBAR_TRACK_HEIGHT;
        int visibleLines   = _layout.NARRATIVE_HEIGHT - _layout.SEPARATOR_HEIGHT;
        int relativeY      = Math.Clamp(mouseY - _layout.CONTENT_START_Y, 0, trackHeight - 1);
        int maxScrollOffset = Math.Max(0, totalLines - visibleLines + 5);
        float scrollRatio  = (float)relativeY / (trackHeight - 1);
        return Math.Clamp((int)(maxScrollOffset * scrollRatio), 0, maxScrollOffset);
    }

    // ── Loading indicator ─────────────────────────────────────────────────────

    /// <summary>Animate a spinner + progress bar centred in the content area.</summary>
    public virtual void ShowLoadingIndicator(string message = "Loading...")
    {
        if ((DateTime.Now - _lastFrameUpdate).TotalMilliseconds > 100)
        {
            _loadingFrameIndex = (_loadingFrameIndex + 1) % Config.Symbols.LoadingSpinner.Length;
            _lastFrameUpdate   = DateTime.Now;
        }

        string spinner = Config.Symbols.LoadingSpinner[_loadingFrameIndex];

        ClearContent();

        string loadingText = $"{spinner}  {message}  {spinner}";
        int    centerY     = _layout.CONTENT_START_Y + _layout.NARRATIVE_HEIGHT / 2;
        int    centerX     = (_layout.TERMINAL_WIDTH - loadingText.Length) / 2;
        _terminal.Text(centerX, centerY, loadingText, Config.Colors.DarkYellowGrey, Config.NarrativeUI.BackgroundColor);

        string dots  = new string('.', _loadingFrameIndex % 4);
        string hint  = $"Please wait {dots}";
        _terminal.Text((_layout.TERMINAL_WIDTH - hint.Length) / 2, centerY + 2, hint,
            Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);

        string bar = GenerateProgressBar(30, _loadingFrameIndex);
        _terminal.Text((_layout.TERMINAL_WIDTH - 30) / 2, centerY - 2, bar,
            Config.NarrativeUI.LoadingColor, Config.NarrativeUI.BackgroundColor);
    }

    // ── Error display ─────────────────────────────────────────────────────────

    /// <summary>Show a centred error message in the content area.</summary>
    public virtual void ShowError(string errorMessage)
    {
        ClearContent();

        int titleY = _layout.CONTENT_START_Y + _layout.NARRATIVE_HEIGHT / 2 - 2;
        _terminal.Text((_layout.TERMINAL_WIDTH - 5) / 2, titleY, "ERROR",
            Config.NarrativeUI.ErrorColor, Config.NarrativeUI.BackgroundColor);

        var lines = WrapText(errorMessage, _layout.CONTENT_WIDTH - 4);
        for (int i = 0; i < lines.Count && titleY + 2 + i < _layout.SEPARATOR_Y; i++)
        {
            string line = lines[i];
            _terminal.Text((_layout.TERMINAL_WIDTH - line.Length) / 2, titleY + 2 + i,
                line, Config.NarrativeUI.ErrorColor, Config.NarrativeUI.BackgroundColor);
        }

        string hint = "(Press ESC to return)";
        _terminal.Text((_layout.TERMINAL_WIDTH - hint.Length) / 2, _layout.SEPARATOR_Y - 1,
            hint, Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);
    }

    // ── Status bar ────────────────────────────────────────────────────────────

    /// <summary>Draw the horizontal separator and write a message to the status bar row.</summary>
    protected void DrawStatusBar(string message)
    {
        DrawHorizontalLine(_layout.SEPARATOR_Y);
        int maxW = _layout.CONTENT_WIDTH - 2;
        if (message.Length > maxW) message = message[..(maxW - 3)] + "...";
        _terminal.Text(_layout.CONTENT_START_X, _layout.STATUS_BAR_Y,
            message, Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);
    }

    // ── Text helpers ──────────────────────────────────────────────────────────

    protected List<string> WrapText(string text, int maxWidth)
    {
        var lines = new List<string>();
        if (string.IsNullOrEmpty(text)) return lines;

        foreach (var paragraph in text.Split(new[] { '\n', '\r' }, StringSplitOptions.None))
        {
            if (string.IsNullOrWhiteSpace(paragraph)) { lines.Add(""); continue; }

            var words   = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var current = new System.Text.StringBuilder();

            foreach (var word in words)
            {
                string test = current.Length == 0 ? word : current + " " + word;
                if (test.Length <= maxWidth)
                {
                    if (current.Length > 0) current.Append(' ');
                    current.Append(word);
                }
                else
                {
                    if (current.Length > 0) { lines.Add(current.ToString()); current.Clear(); }
                    if (word.Length > maxWidth) { lines.Add(word[..maxWidth]); current.Append(word[maxWidth..]); }
                    else current.Append(word);
                }
            }
            if (current.Length > 0) lines.Add(current.ToString());
        }
        return lines;
    }

    private string GenerateProgressBar(int width, int frame)
    {
        var    bar   = new System.Text.StringBuilder("[");
        string chars = " ░░▒▒▓█▓▒▒░░";
        for (int i = 0; i < width - 2; i++) bar.Append(chars[(frame + i) % chars.Length]);
        bar.Append(']');
        return bar.ToString();
    }

    // ── Dice-roll overlay (shared with NarrativeUI and DialogueUI) ────────────

    /// <summary>
    /// Render a <see cref="DiceRollComponent"/> centered in this panel's content area.
    /// Clears the content first, then delegates animation, dice, humor buttons, and the
    /// [ Continue ] button to the component. Returns true once the continue button is visible.
    /// Hit-test clicks against <see cref="DiceRollComponent.ContinueButtonRegion"/> and route
    /// hover/click to <see cref="DiceRollComponent.HandleHumorHover"/> /
    /// <see cref="DiceRollComponent.HandleHumorClick"/>.
    /// </summary>
    public bool RenderDiceComponent(DiceRollComponent dice, bool continueHovered)
    {
        ClearContent();
        int centerX = _layout.TERMINAL_WIDTH / 2;
        int centerY = _layout.CONTENT_START_Y + _layout.NARRATIVE_HEIGHT / 2;
        return dice.Render(_terminal, centerX, centerY, continueHovered);
    }
}
