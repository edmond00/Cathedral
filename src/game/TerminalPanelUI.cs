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

    // ── Dice-roll animation fields ────────────────────────────────────────────
    private int[]  _rollingDiceFrames = Array.Empty<int>();
    private bool[] _diceShowingFaces  = Array.Empty<bool>();
    private int[]  _diceFrameCounters = Array.Empty<int>();
    private int[]  _diceWaitTimes     = Array.Empty<int>();
    private readonly Random _diceRandom = new();
    private (int X, int Y, int Width) _diceRollButtonRegion;

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
        if (totalLines <= visibleLines) return (0, 0);

        float visibleRatio = (float)visibleLines / totalLines;
        int   thumbHeight  = Math.Max(2, (int)(trackHeight * visibleRatio));
        int   maxScrollOff = _layout.CalculateMaxScrollOffset(totalLines);
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

    // ── Dice-roll indicator (shared with NarrativeUI and DialogueUI) ──────────

    /// <summary>
    /// Renders the full N-dice roll screen. Animates while rolling; shows final values
    /// with a [ Continue ] button once complete. Returns true when the continue button
    /// is visible so the caller knows clicks should be tested with
    /// <see cref="IsMouseOverDiceRollButton"/>.
    /// </summary>
    public bool ShowDiceRollIndicator(
        int numberOfDice,
        int difficulty,
        bool isRolling,
        int[]? finalDiceValues = null,
        bool isContinueButtonHovered = false)
    {
        // Advance animation every 80 ms
        if ((DateTime.Now - _lastFrameUpdate).TotalMilliseconds > 80)
        {
            _loadingFrameIndex = (_loadingFrameIndex + 1) % Config.Symbols.DiceRollingFrames.Length;
            _lastFrameUpdate   = DateTime.Now;

            if (isRolling)
            {
                if (_rollingDiceFrames.Length != numberOfDice)
                {
                    _rollingDiceFrames = new int[numberOfDice];
                    _diceShowingFaces  = new bool[numberOfDice];
                    _diceFrameCounters = new int[numberOfDice];
                    _diceWaitTimes     = new int[numberOfDice];
                    for (int i = 0; i < numberOfDice; i++)
                    {
                        _diceShowingFaces[i]  = _diceRandom.Next(2) == 0;
                        _diceFrameCounters[i] = 0;
                        _diceWaitTimes[i]     = _diceRandom.Next(1, 4);
                        _rollingDiceFrames[i] = _diceShowingFaces[i]
                            ? _diceRandom.Next(Config.Symbols.DiceFaces.Length)
                            : _diceRandom.Next(Config.Symbols.DiceSideViews.Length);
                    }
                }

                for (int i = 0; i < _rollingDiceFrames.Length; i++)
                {
                    _diceFrameCounters[i]++;
                    if (_diceFrameCounters[i] >= _diceWaitTimes[i])
                    {
                        _diceFrameCounters[i] = 0;
                        _diceWaitTimes[i]     = _diceRandom.Next(1, 6);
                        _diceShowingFaces[i]  = !_diceShowingFaces[i];
                        _rollingDiceFrames[i] = _diceShowingFaces[i]
                            ? _diceRandom.Next(Config.Symbols.DiceFaces.Length)
                            : _diceRandom.Next(Config.Symbols.DiceSideViews.Length);
                    }
                }
            }
        }

        // Initialise frames if not yet sized (first call before first tick)
        if (_rollingDiceFrames.Length != numberOfDice)
        {
            _rollingDiceFrames = new int[numberOfDice];
            _diceShowingFaces  = new bool[numberOfDice];
            _diceFrameCounters = new int[numberOfDice];
            _diceWaitTimes     = new int[numberOfDice];
            for (int i = 0; i < numberOfDice; i++)
            {
                _diceShowingFaces[i]  = _diceRandom.Next(2) == 0;
                _diceFrameCounters[i] = 0;
                _diceWaitTimes[i]     = _diceRandom.Next(1, 6);
                _rollingDiceFrames[i] = _diceShowingFaces[i]
                    ? _diceRandom.Next(Config.Symbols.DiceFaces.Length)
                    : _diceRandom.Next(Config.Symbols.DiceSideViews.Length);
            }
        }

        // Clear content area
        ClearContent();

        int centerY = _layout.CONTENT_START_Y + _layout.NARRATIVE_HEIGHT / 2;

        // Results (used in multiple places below)
        int  numberOfSixes = 0;
        bool isSuccess     = false;
        if (!isRolling && finalDiceValues != null)
        {
            numberOfSixes = finalDiceValues.Count(v => v == 6);
            isSuccess     = numberOfSixes >= difficulty;
        }

        // Title
        string  title      = isRolling ? "Rolling Dice..." : (isSuccess ? "SUCCESS!" : "FAILURE!");
        Vector4 titleColor = isRolling
            ? Config.NarrativeUI.LoadingColor
            : (isSuccess ? Config.NarrativeUI.SuccessColor : Config.NarrativeUI.FailureColor);
        _terminal.Text((_layout.TERMINAL_WIDTH - title.Length) / 2, centerY - 10,
            title, titleColor, Config.NarrativeUI.BackgroundColor);

        // Difficulty indicator
        int  diffClamp   = Math.Clamp(difficulty, 1, 10);
        char diffGlyph   = Config.Symbols.DifficultyGlyphs[diffClamp - 1];
        float diffRatio  = (diffClamp - 1) / 9.0f;
        var   diffColor  = new Vector4(
            1.0f + (Config.Colors.DarkYellow.X - 1.0f) * diffRatio,
            1.0f + (Config.Colors.DarkYellow.Y - 1.0f) * diffRatio,
            1.0f + (Config.Colors.DarkYellow.Z - 1.0f) * diffRatio,
            1.0f);
        string diffLabel  = $"Difficulty: {diffGlyph} ({diffClamp} {(diffClamp == 1 ? "six" : "sixes")} needed)";
        int    diffLabelX = (_layout.TERMINAL_WIDTH - diffLabel.Length) / 2;
        int    diffLabelY = centerY - 8;
        _terminal.Text(diffLabelX,      diffLabelY, "Difficulty: ",             Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);
        _terminal.Text(diffLabelX + 12, diffLabelY, diffGlyph.ToString(),       diffColor,                         Config.NarrativeUI.BackgroundColor);
        string diffSuffix = $" ({diffClamp} {(diffClamp == 1 ? "six" : "sixes")} needed)";
        _terminal.Text(diffLabelX + 13, diffLabelY, diffSuffix,                 Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);

        // Dice grid
        int dicePerRow    = Math.Min(numberOfDice, 20);
        int startX        = (_layout.TERMINAL_WIDTH - dicePerRow * 2) / 2;
        int startY        = centerY - 5;

        for (int i = 0; i < numberOfDice; i++)
        {
            int row   = (i / dicePerRow) * 2;
            int col   = i % dicePerRow;
            int diceX = startX + col * 2 + (row % 2);
            int diceY = startY + row;

            char    diceChar;
            Vector4 diceColor;

            if (isRolling)
            {
                diceChar  = _diceShowingFaces[i]
                    ? Config.Symbols.DiceFaces[_rollingDiceFrames[i]]
                    : Config.Symbols.DiceSideViews[_rollingDiceFrames[i]];
                diceColor = Config.NarrativeUI.LoadingColor;
            }
            else if (finalDiceValues != null && i < finalDiceValues.Length)
            {
                int value = Math.Clamp(finalDiceValues[i], 1, 6);
                diceChar  = Config.Symbols.DiceFaces[value - 1];
                diceColor = value == 6 ? Config.NarrativeUI.DiceGoldColor : Config.NarrativeUI.NarrativeColor;
            }
            else
            {
                diceChar  = Config.Symbols.DiceFaces[0];
                diceColor = Config.NarrativeUI.NarrativeColor;
            }

            _terminal.SetCell(diceX, diceY, diceChar, diceColor, Config.NarrativeUI.BackgroundColor);
        }

        int rows   = ((numberOfDice + dicePerRow - 1) / dicePerRow) * 2;
        int afterY = startY + rows + 2;

        if (!isRolling && finalDiceValues != null)
        {
            // Summary
            string  summary  = $"Rolled {numberOfSixes} {(numberOfSixes == 1 ? "six" : "sixes")} out of {numberOfDice} dice";
            Vector4 summCol  = isSuccess ? Config.NarrativeUI.SuccessColor : Config.NarrativeUI.FailureColor;
            _terminal.Text((_layout.TERMINAL_WIDTH - summary.Length) / 2, afterY, summary, summCol, Config.NarrativeUI.BackgroundColor);

            // Continue button
            string  btn    = "[ Continue ]";
            int     btnX   = (_layout.TERMINAL_WIDTH - btn.Length) / 2;
            int     btnY   = afterY + 3;
            Vector4 btnFg  = isContinueButtonHovered ? Config.NarrativeUI.ContinueButtonHoverColor      : Config.NarrativeUI.ContinueButtonColor;
            Vector4 btnBg  = isContinueButtonHovered ? Config.NarrativeUI.ContinueButtonHoverBackgroundColor : Config.NarrativeUI.ContinueButtonBackgroundColor;
            _terminal.Text(btnX, btnY, btn, btnFg, btnBg);
            _diceRollButtonRegion = (btnX, btnY, btn.Length);
            return true;
        }
        else
        {
            string spinner = Config.Symbols.LoadingSpinner[_loadingFrameIndex % Config.Symbols.LoadingSpinner.Length];
            string wait    = $"{spinner}  Please wait...  {spinner}";
            _terminal.Text((_layout.TERMINAL_WIDTH - wait.Length) / 2, afterY, wait,
                Config.Colors.DarkYellowGrey, Config.NarrativeUI.BackgroundColor);
            return false;
        }
    }

    /// <summary>Returns true if the mouse is over the dice-roll continue button.</summary>
    public bool IsMouseOverDiceRollButton(int mouseX, int mouseY)
        => mouseY == _diceRollButtonRegion.Y
        && mouseX >= _diceRollButtonRegion.X
        && mouseX <  _diceRollButtonRegion.X + _diceRollButtonRegion.Width;
}
