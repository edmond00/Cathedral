using System;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;

namespace Cathedral.Game;

/// <summary>
/// Standalone dice-roll animation component for use in any OpenGL window / terminal.
/// Mirrors the dice logic from <c>NarrativeUI.ShowDiceRollIndicator</c> but is
/// decoupled from NarrativeUI's layout so fight mode can use it independently.
///
/// Lifecycle:
///   1. Call <see cref="Start"/> to reset into rolling mode.
///   2. Call <see cref="Advance"/> every update frame while <see cref="IsRolling"/> is true.
///   3. Call <see cref="Complete"/> with the final dice values when the caller decides to stop.
///   4. Call <see cref="Render"/> every render frame; it returns <c>true</c> once the
///      "Continue" button is drawn (happens only after <see cref="Complete"/> is called).
///   5. Call <see cref="Hide"/> after the player confirms to clear state.
/// </summary>
public class DiceRollComponent
{
    // ── Per-die animation state ───────────────────────────────────────
    private int[] _rollingDiceFrames = Array.Empty<int>();
    private bool[] _diceShowingFaces = Array.Empty<bool>();
    private int[] _diceFrameCounters = Array.Empty<int>();
    private int[] _diceWaitTimes    = Array.Empty<int>();
    private readonly Random _rng    = new();

    // ── Animation timing ──────────────────────────────────────────────
    private DateTime _lastFrameUpdate = DateTime.MinValue;
    private int _spinnerFrame;

    // ── Public state ─────────────────────────────────────────────────
    public int NumberOfDice { get; private set; }
    public int Difficulty   { get; private set; }

    public bool IsRolling  { get; private set; }
    public bool IsVisible  { get; private set; }

    /// <summary>
    /// Region of the "[ Continue ]" button from the last <see cref="Render"/> call.
    /// Valid only when <see cref="IsRolling"/> is false and the button is drawn.
    /// </summary>
    public (int X, int Y, int Width) ContinueButtonRegion { get; private set; }

    private int[]? _finalValues;

    // ── Lifecycle ─────────────────────────────────────────────────────

    /// <summary>Begin a new dice-roll animation.</summary>
    public void Start(int numberOfDice, int difficulty)
    {
        NumberOfDice = Math.Max(1, numberOfDice);
        Difficulty   = Math.Max(1, difficulty);
        IsRolling    = true;
        IsVisible    = true;
        _finalValues = null;
        ContinueButtonRegion = default;
        _spinnerFrame = 0;
        _lastFrameUpdate = DateTime.MinValue;
        InitDiceArrays();
    }

    /// <summary>Stop the rolling animation and lock in the result.</summary>
    public void Complete(int[] finalValues)
    {
        _finalValues = finalValues;
        IsRolling    = false;
    }

    /// <summary>Hide the component (player confirmed result).</summary>
    public void Hide()
    {
        IsVisible    = false;
        IsRolling    = false;
        _finalValues = null;
    }

    /// <summary>
    /// Advance the rolling animation state. Call once per update frame while <see cref="IsRolling"/>.
    /// </summary>
    public void Advance()
    {
        if (!IsRolling) return;

        if ((DateTime.Now - _lastFrameUpdate).TotalMilliseconds > 80)
        {
            _lastFrameUpdate = DateTime.Now;
            _spinnerFrame++;

            // Update each die independently
            for (int i = 0; i < _rollingDiceFrames.Length; i++)
            {
                _diceFrameCounters[i]++;
                if (_diceFrameCounters[i] < _diceWaitTimes[i]) continue;

                _diceFrameCounters[i] = 0;
                _diceWaitTimes[i] = _rng.Next(1, 6);
                _diceShowingFaces[i] = !_diceShowingFaces[i];
                _rollingDiceFrames[i] = _diceShowingFaces[i]
                    ? _rng.Next(Config.Symbols.DiceFaces.Length)
                    : _rng.Next(Config.Symbols.DiceSideViews.Length);
            }
        }
    }

    // ── Rendering ─────────────────────────────────────────────────────

    /// <summary>
    /// Render the dice roll overlay centered at (<paramref name="centerX"/>, <paramref name="centerY"/>).
    /// Returns <c>true</c> when the "Continue" button is visible (rolling is done).
    /// </summary>
    public bool Render(TerminalHUD terminal, int centerX, int centerY, bool isContinueHovered)
    {
        if (!IsVisible) return false;

        // ── Black background panel ───────────────────────────────────────
        int dpr = Math.Min(NumberOfDice, 20);
        int bgRows = ((NumberOfDice + dpr - 1) / dpr) * 2;
        int bgW = 54, bgH = 18 + bgRows;
        int bgX = centerX - bgW / 2;
        int bgY = centerY - 13;
        terminal.FillRect(bgX, bgY, bgW, bgH, ' ', Config.Colors.White, Config.Colors.Black);
        terminal.DrawBox(bgX, bgY, bgW, bgH, BoxStyle.Single, Config.Colors.DarkYellowGrey, Config.Colors.Black);

        bool hasFinal = !IsRolling && _finalValues != null;
        int sixesCount = hasFinal ? _finalValues!.Count(v => v == 6) : 0;
        bool isSuccess = sixesCount > Difficulty; // strictly greater-than (design choice)

        // ── Title ───────────────────────────────────────────────────────
        string title     = IsRolling ? "Rolling Dice..." : (isSuccess ? "SUCCESS!" : "FAILURE!");
        Vector4 titleCol = IsRolling
            ? Config.Colors.Yellow
            : (isSuccess ? Config.Colors.LightGreen : Config.Colors.Red);
        int titleY = centerY - 10;
        int titleX = centerX - title.Length / 2;
        terminal.Text(titleX, titleY, title, titleCol, Config.Colors.Black);

        // ── Difficulty line ─────────────────────────────────────────────
        int diffClamp = Math.Clamp(Difficulty, 1, 10);
        char diffGlyph = Config.Symbols.DifficultyGlyphs[diffClamp - 1];
        string diffLabel = $"Difficulty: {diffGlyph} ({diffClamp} sixes needed to hit)";
        int diffY = centerY - 8;
        int diffX = centerX - diffLabel.Length / 2;
        terminal.Text(diffX, diffY, diffLabel, Config.Colors.DarkYellowGrey, Config.Colors.Black);

        // ── Dice grid ───────────────────────────────────────────────────
        int dicePerRow = Math.Min(NumberOfDice, 20);
        int diceSpacing = 2;
        int startX = centerX - (dicePerRow * diceSpacing) / 2;
        int startY = centerY - 5;

        for (int i = 0; i < NumberOfDice; i++)
        {
            int row  = (i / dicePerRow) * 2;
            int col  = i % dicePerRow;
            int dx   = startX + col * diceSpacing + (row % 2);
            int dy   = startY + row;

            char diceChar;
            Vector4 diceColor;

            if (IsRolling)
            {
                diceChar  = _diceShowingFaces[i]
                    ? Config.Symbols.DiceFaces[_rollingDiceFrames[i]]
                    : Config.Symbols.DiceSideViews[_rollingDiceFrames[i]];
                diceColor = Config.Colors.Yellow;
            }
            else if (_finalValues != null && i < _finalValues.Length)
            {
                int val = Math.Clamp(_finalValues[i], 1, 6);
                diceChar  = Config.Symbols.DiceFaces[val - 1];
                diceColor = val == 6 ? Config.Colors.GoldYellow : Config.Colors.DarkYellowGrey;
            }
            else
            {
                diceChar  = Config.Symbols.DiceFaces[0];
                diceColor = Config.Colors.DarkYellowGrey;
            }

            terminal.SetCell(dx, dy, diceChar, diceColor, Config.Colors.Black);
        }

        int rowsDrawn = ((NumberOfDice + dicePerRow - 1) / dicePerRow) * 2;

        // ── Result summary or spinner ────────────────────────────────────
        int summaryY = startY + rowsDrawn + 2;

        if (hasFinal && _finalValues != null)
        {
            string summary  = $"Rolled {sixesCount} {(sixesCount == 1 ? "six" : "sixes")} out of {NumberOfDice} dice";
            int summaryX    = centerX - summary.Length / 2;
            Vector4 sumCol  = isSuccess ? Config.Colors.LightGreen : Config.Colors.Red;
            terminal.Text(summaryX, summaryY, summary, sumCol, Config.Colors.Black);

            // ── Continue button ─────────────────────────────────────────
            const string btnText = "[ Continue ]";
            int btnX   = centerX - btnText.Length / 2;
            int btnY   = summaryY + 3;
            var btnFg  = isContinueHovered ? Config.Colors.Black     : Config.Colors.White;
            var btnBg  = isContinueHovered ? Config.Colors.White     : Config.Colors.DarkGray;
            terminal.Text(btnX, btnY, btnText, btnFg, btnBg);
            ContinueButtonRegion = (btnX, btnY, btnText.Length);
            return true;
        }
        else
        {
            // Spinner while rolling
            string spinner = Config.Symbols.LoadingSpinner[_spinnerFrame % Config.Symbols.LoadingSpinner.Length];
            string waitMsg = $"{spinner}  Please wait...  {spinner}";
            int waitX = centerX - waitMsg.Length / 2;
            terminal.Text(waitX, summaryY, waitMsg, Config.Colors.DarkYellowGrey, Config.Colors.Black);
        }

        return false;
    }

    // ── Private ───────────────────────────────────────────────────────

    private void InitDiceArrays()
    {
        _rollingDiceFrames = new int[NumberOfDice];
        _diceShowingFaces  = new bool[NumberOfDice];
        _diceFrameCounters = new int[NumberOfDice];
        _diceWaitTimes     = new int[NumberOfDice];

        for (int i = 0; i < NumberOfDice; i++)
        {
            _diceShowingFaces[i]  = _rng.Next(2) == 0;
            _diceFrameCounters[i] = 0;
            _diceWaitTimes[i]     = _rng.Next(1, 6);
            _rollingDiceFrames[i] = _diceShowingFaces[i]
                ? _rng.Next(Config.Symbols.DiceFaces.Length)
                : _rng.Next(Config.Symbols.DiceSideViews.Length);
        }
    }
}
