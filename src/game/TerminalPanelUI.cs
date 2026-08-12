using System;
using System.Collections.Generic;
using System.Text;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Preview;

namespace Cathedral.Game;

/// <summary>
/// Abstract base class for bordered terminal-panel UIs (NarrativeUI, DialogueUI, …).
/// Encapsulates the shared infrastructure that every panel needs:
///   • Padding-zone border rendering  (Clear / ClearContent)
///   • Horizontal separator lines     (DrawHorizontalLine)
///   • Proportional scrollbar         (RenderScrollbar + hit-test helpers)
///   • Content grey-out               (DimContent)
///   • Animated center progress bar   (RenderCenterProgressBar)
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

        // Both ratios are measured against `totalLines` — the same number the buffer's own
        // MaxScrollOffset works from (NarrationScrollBuffer.TotalLines is _renderedLines.Count).
        //
        // This used to substitute max(totalLines, scrollOffset + visibleLines). Since
        // CalculateMaxScrollOffset adds SCROLL_BOTTOM_MARGIN, the bottom offset already exceeds
        // totalLines - visibleLines, so that substitution added the margin to the denominator a
        // SECOND time and capped the position ratio at maxScroll / (maxScroll + margin). Scrolled
        // fully to the bottom the thumb therefore stopped short of the track's end — a quarter of the
        // way short over a typical buffer — and shrank as it went, since the height ratio was
        // inflated by the same amount.
        float visibleRatio = Math.Clamp((float)visibleLines / totalLines, 0f, 1f);
        int   thumbHeight  = Math.Clamp((int)(trackHeight * visibleRatio), 2, trackHeight);

        int   maxScrollOff = _layout.CalculateMaxScrollOffset(totalLines);
        float scrollRatio  = maxScrollOff > 0 ? Math.Clamp((float)scrollOffset / maxScrollOff, 0f, 1f) : 0f;
        int   thumbY       = trackStartY + (int)Math.Round((trackHeight - thumbHeight) * scrollRatio);

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

    // ── Content grey-out + generating header ──────────────────────────────────

    /// <summary>Grey out every row from <paramref name="startY"/> to the bottom of the panel.</summary>
    private void DimFrom(int startY)
    {
        _terminal.DimRect(
            _layout.LEFT_PADDING,
            startY,
            _layout.TERMINAL_WIDTH - _layout.LEFT_PADDING - _layout.RIGHT_PADDING,
            (_layout.TERMINAL_HEIGHT - _layout.BOTTOM_PADDING) - startY,
            Config.NarrativeUI.DimmedContentColor,
            Config.NarrativeUI.BackgroundColor);
    }

    /// <summary>
    /// Grey out everything inside the panel border (header, content, status bar).
    /// Idempotent — safe to apply every frame. Draw any overlay (dice box, progress bar,
    /// status message) AFTER dimming so it stays at full brightness.
    /// </summary>
    public void DimContent() => DimFrom(_layout.TOP_PADDING);

    /// <summary>
    /// Draw a small centered animated progress bar (bracketed, same width as the old
    /// centre-screen loading bar) in the middle of the content area, in a lighter shade
    /// than <see cref="Config.NarrativeUI.DimmedContentColor"/> so it stands out against
    /// the greyed-out content drawn underneath via <see cref="DimContent"/>.
    /// </summary>
    public void RenderCenterProgressBar()
    {
        const int barWidth = 30;
        int frame = AdvanceSpinnerFrame();

        const string chars = " ░░▒▒▓█▓▒▒░░";
        var bar = new System.Text.StringBuilder("[");
        for (int i = 0; i < barWidth - 2; i++)
            bar.Append(chars[(frame + i) % chars.Length]);
        bar.Append(']');

        int barX = (_layout.TERMINAL_WIDTH - barWidth) / 2;
        int barY = _layout.CONTENT_START_Y + _layout.NARRATIVE_HEIGHT / 2;
        _terminal.Text(barX, barY, bar.ToString(), Config.Colors.LightGray75, Config.NarrativeUI.BackgroundColor);
    }

    /// <summary>
    /// Draw the status bar with the current loading message, its trailing ellipsis animated
    /// (0–3 dots cycling) instead of static — e.g. "The senses wander", "The senses wander.",
    /// "The senses wander..". Any literal trailing dots/ellipsis on <paramref name="message"/>
    /// are stripped first so the animation is the only source of "...". Used at the bottom of the
    /// panel while the LLM is generating, alongside <see cref="RenderCenterProgressBar"/>.
    /// </summary>
    public void RenderWaitingStatus(string message)
    {
        int frame = AdvanceSpinnerFrame();
        string spinner = Config.Symbols.LoadingSpinner[frame];
        string baseMsg = message.TrimEnd('.', ' ', '…');
        string dots    = new string('.', frame % 4);
        DrawStatusBar($"{spinner} {baseMsg}{dots}", Config.Colors.LightGray75);
    }

    /// <summary>Advance the shared spinner animation at ~10 fps and return the current frame index.</summary>
    protected int AdvanceSpinnerFrame()
    {
        if ((DateTime.Now - _lastFrameUpdate).TotalMilliseconds > 100)
        {
            _loadingFrameIndex = (_loadingFrameIndex + 1) % Config.Symbols.LoadingSpinner.Length;
            _lastFrameUpdate   = DateTime.Now;
        }
        return _loadingFrameIndex;
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
        => DrawStatusBar(message, Config.NarrativeUI.StatusBarColor);

    /// <summary>Draw the horizontal separator and write a message to the status bar row in a given color.</summary>
    protected void DrawStatusBar(string message, Vector4 textColor)
    {
        DrawHorizontalLine(_layout.SEPARATOR_Y);
        int maxW = _layout.CONTENT_WIDTH - 2;
        if (message.Length > maxW) message = message[..(maxW - 3)] + "...";
        _terminal.Text(_layout.CONTENT_START_X, _layout.STATUS_BAR_Y,
            message, textColor, Config.NarrativeUI.BackgroundColor);
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

    // ── Footer exit button ────────────────────────────────────────────────────

    /// <summary>
    /// Renders the single footer button (e.g. "CONTINUE", "LEAVE", "RUNAWAY", "INTERRUPT")
    /// at the bottom-left, above the separator, and returns its click region.
    /// </summary>
    public (int X, int Y, int Width) RenderExitButton(string label, bool isHovered = false)
    {
        string buttonText = $"[ {label} ]";
        int buttonX = _layout.CONTENT_START_X;   // bottom-left
        int buttonY = _layout.SEPARATOR_Y - 2;

        Vector4 buttonColor           = isHovered ? Config.NarrativeUI.ContinueButtonHoverColor           : Config.NarrativeUI.ContinueButtonColor;
        Vector4 buttonBackgroundColor = isHovered ? Config.NarrativeUI.ContinueButtonHoverBackgroundColor : Config.NarrativeUI.ContinueButtonBackgroundColor;

        _terminal.Text(buttonX, buttonY, buttonText, buttonColor, buttonBackgroundColor);

        return (buttonX, buttonY, buttonText.Length);
    }

    // ── Outcome report chips (shared with NarrativeUI and DialogueTreeUI) ─────

    /// <summary>
    /// Draw one outcome-report chip: the pre-centred text of <paramref name="line"/> on a band
    /// coloured by severity (positive / negative / neutral). Shared so a dialogue's outcome is
    /// indistinguishable from an action's outcome — they are the same kind of event to the player.
    /// <paramref name="flatColor"/> replaces the band with plain trimmed text in that colour, which
    /// is how both panels render a chip that has greyed into history or been dimmed behind an overlay.
    /// </summary>
    protected void RenderReportChip(RenderedLine line, int y, Vector4? flatColor = null)
    {
        if (line.Report == null) return;

        if (flatColor is { } flat)
        {
            _terminal.Text(_layout.CONTENT_START_X, y, line.Text.TrimEnd(), flat,
                Config.NarrativeUI.BackgroundColor);
            return;
        }

        var bg = line.Report.Severity switch
        {
            OutcomeSeverity.Negative => Config.NarrativeUI.OutcomeReportNegativeBackground,
            OutcomeSeverity.Positive => Config.NarrativeUI.OutcomeReportPositiveBackground,
            _                              => Config.NarrativeUI.OutcomeReportNeutralBackground,
        };
        _terminal.Text(_layout.CONTENT_START_X, y, line.Text,
            Config.NarrativeUI.OutcomeReportTextColor, bg);
    }

    // ── Dice-roll overlay (shared with NarrativeUI and DialogueUI) ────────────

    /// <summary>
    /// Render a <see cref="DiceRollComponent"/> as an overlay box centered in this panel's
    /// content area. The underlying content is greyed out first (matching fight mode), then
    /// animation, dice, humor buttons, and the [ Continue ] button are delegated to the
    /// component. Returns true once the continue button is visible.
    /// Hit-test clicks against <see cref="DiceRollComponent.ContinueButtonRegion"/> and route
    /// hover/click to <see cref="DiceRollComponent.HandleHumorHover"/> /
    /// <see cref="DiceRollComponent.HandleHumorClick"/>.
    /// </summary>
    public bool RenderDiceComponent(DiceRollComponent dice, bool continueHovered)
    {
        DimContent();
        int centerX = _layout.TERMINAL_WIDTH / 2;
        int centerY = _layout.CONTENT_START_Y + _layout.NARRATIVE_HEIGHT / 2;
        return dice.Render(_terminal, centerX, centerY, continueHovered);
    }

    // ── LLM generation preview overlay (replaces the centre progress bar) ─────────

    /// <summary>
    /// Draws the "text being generated" preview box: a thin-grey-bordered, black-filled overlay ~half
    /// the menu size, centred over the greyed-out content. Layout top→bottom: the modus-mentis title
    /// centred on the top border; a blank row; the animated progress bar; a blank row; the streamed
    /// preview text (tail-clipped); then either a <c>.</c>/<c>..</c>/<c>...</c> dot animation (still
    /// generating) or a <c>[ CONTINUE ]</c> button (done). Returns the CONTINUE hit-region for the
    /// controller to store and click-test — <c>Present=false</c> while generation is still in flight.
    /// </summary>
    public (bool Present, int X, int Y, int Width) RenderPreviewBox(PreviewSnapshot state, bool continueHovered)
    {
        if (!state.Active) return (false, 0, 0, 0);

        DimContent();

        Vector4 border = Config.NarrativeUI.SeparatorColor;
        Vector4 black  = Config.NarrativeUI.BackgroundColor;

        int bgW = Math.Max(28, _layout.CONTENT_WIDTH  / 2);
        int bgH = Math.Max(11, _layout.NARRATIVE_HEIGHT / 2);
        int centerX = _layout.TERMINAL_WIDTH / 2;
        int centerY = _layout.CONTENT_START_Y + _layout.NARRATIVE_HEIGHT / 2;
        int bgX = centerX - bgW / 2;
        int bgY = centerY - bgH / 2;

        _terminal.FillRect(bgX, bgY, bgW, bgH, ' ', Config.Colors.White, black);
        _terminal.DrawBox(bgX, bgY, bgW, bgH, BoxStyle.Single, border, black);

        // Title centred on the top border line.
        if (!string.IsNullOrEmpty(state.Title))
            _terminal.Text(bgX + bgW / 2, bgY, $" {state.Title} ",
                Config.NarrativeUI.ModusMentisHeaderColor, black, TextAlignment.Center);

        int innerX = bgX + 2;
        int innerW = bgW - 4;
        int frame  = AdvanceSpinnerFrame();

        // Progress bar on the second interior row (border + one blank row above it).
        const string barChars = " ░░▒▒▓█▓▒▒░░";
        var bar = new StringBuilder();
        for (int i = 0; i < innerW; i++) bar.Append(barChars[(frame + i) % barChars.Length]);
        _terminal.Text(innerX, bgY + 2, bar.ToString(), Config.Colors.LightGray75, black);

        // Preview text: blank row (bgY+3), then wrapped text from bgY+4 down to a guaranteed blank row
        // above the button.
        int textStartY = bgY + 4;
        int buttonY    = bgY + bgH - 3; // one row up from the bottom border
        int textEndY   = buttonY - 2; // leave (at least) one blank row above the button
        int maxRows    = Math.Max(0, textEndY - textStartY + 1);

        // Wrap each segment to the box width, tagging every wrapped line with its kind so free reasoning
        // renders dimmer. Free reasoning (the persona's unconstrained inner thought) is parenthesized by
        // the snapshot already.
        var lines = new List<(string Text, bool IsFree)>();
        foreach (var seg in state.Segments)
            foreach (var wl in WrapText(seg.Text, innerW))
                lines.Add((wl, seg.IsFree));

        // Auto-scroll: drop the oldest lines so the most recently streamed text stays visible; the
        // clip to maxRows keeps the blank row above the button intact even as the text grows.
        int first = Math.Max(0, lines.Count - maxRows);
        for (int i = first; i < lines.Count; i++)
        {
            Vector4 col = lines[i].IsFree ? Config.Colors.DarkGray35 : Config.NarrativeUI.NarrativeColor;
            _terminal.Text(innerX, textStartY + (i - first), lines[i].Text, col, black);
        }

        if (state.Complete)
        {
            string btn = "[ CONTINUE ]";
            int btnX = centerX - btn.Length / 2;
            Vector4 fg = continueHovered ? Config.NarrativeUI.ContinueButtonHoverColor           : Config.NarrativeUI.ContinueButtonColor;
            Vector4 bg = continueHovered ? Config.NarrativeUI.ContinueButtonHoverBackgroundColor : Config.NarrativeUI.ContinueButtonBackgroundColor;
            _terminal.Text(btnX, buttonY, btn, fg, bg);
            return (true, btnX, buttonY, btn.Length);
        }

        // Still generating → dot animation where the button will be.
        string dots = new string('.', 1 + (frame % 3));
        _terminal.Text(centerX - 1, buttonY, dots.PadRight(3), Config.Colors.LightGray75, black);
        return (false, 0, 0, 0);
    }
}
