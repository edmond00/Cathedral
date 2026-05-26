using System;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;

namespace Cathedral.Game;

/// <summary>
/// Standalone dice-roll animation component for use in any OpenGL window / terminal.
/// Supports two modes:
///   • Single-roll (learning checks, narrative checks): one dice group vs a static difficulty.
///   • Dual-roll (attack vs defense): two dice groups stacked; success = primary sixes &gt; secondary sixes.
///
/// Lifecycle:
///   1. Call <see cref="Start"/> or <see cref="StartDual"/> to reset into rolling mode.
///   2. Call <see cref="Advance"/> every update frame while <see cref="IsRolling"/> is true.
///   3. Call <see cref="Complete"/> or <see cref="CompleteDual"/> with the final dice values.
///   4. Call <see cref="Render"/> every render frame; returns <c>true</c> once "Continue" is drawn.
///   5. Call <see cref="Hide"/> after the player confirms to clear state.
/// </summary>
public class DiceRollComponent
{
    // ── Primary dice (attack / single roll) ──────────────────────────
    private int[]  _primaryFrames    = Array.Empty<int>();
    private bool[] _primaryFaces     = Array.Empty<bool>();
    private int[]  _primaryCounters  = Array.Empty<int>();
    private int[]  _primaryWaits     = Array.Empty<int>();

    // ── Secondary dice (defense — only used in dual-roll mode) ───────
    private int[]  _secondaryFrames   = Array.Empty<int>();
    private bool[] _secondaryFaces    = Array.Empty<bool>();
    private int[]  _secondaryCounters = Array.Empty<int>();
    private int[]  _secondaryWaits    = Array.Empty<int>();

    private readonly Random _rng = new();

    // ── Animation timing ──────────────────────────────────────────────
    private DateTime _lastFrameUpdate = DateTime.MinValue;
    private int _spinnerFrame;

    // ── Public state ─────────────────────────────────────────────────
    public int NumberOfDice          { get; private set; }
    public int Difficulty            { get; private set; }
    public int SecondaryNumberOfDice { get; private set; }

    public bool IsRolling { get; private set; }
    public bool IsVisible { get; private set; }
    public bool IsDual    { get; private set; }

    public (int X, int Y, int Width) ContinueButtonRegion { get; private set; }

    public string? Subtitle         { get; private set; }
    public string  DifficultyVerb   { get; private set; } = "to hit";
    public string? PrimaryLabel     { get; private set; }
    public string? SecondaryLabel   { get; private set; }
    public Vector4? AccentColor     { get; private set; }

    /// <summary>
    /// Fires every time at least one die visibly changes face during the rolling animation.
    /// Hook a short PCM tick to evoke the rattle of tumbling dice.
    /// </summary>
    public Action? OnDiceTick { get; set; }

    private int[]? _finalPrimary;
    private int[]? _finalSecondary;

    // ── Lifecycle ─────────────────────────────────────────────────────

    /// <summary>Begin a new single-dice animation (learning, narrative check).</summary>
    public void Start(int numberOfDice, int difficulty,
                       string? subtitle = null,
                       string difficultyVerb = "to hit",
                       Vector4? accentColor = null)
    {
        NumberOfDice          = Math.Max(1, numberOfDice);
        SecondaryNumberOfDice = 0;
        Difficulty            = Math.Max(1, difficulty);
        IsRolling             = true;
        IsVisible             = true;
        IsDual                = false;
        _finalPrimary         = null;
        _finalSecondary       = null;
        ContinueButtonRegion  = default;
        _spinnerFrame         = 0;
        _lastFrameUpdate      = DateTime.MinValue;
        Subtitle              = subtitle;
        DifficultyVerb        = difficultyVerb;
        PrimaryLabel          = null;
        SecondaryLabel        = null;
        AccentColor           = accentColor;

        InitArrays(NumberOfDice, out _primaryFrames, out _primaryFaces, out _primaryCounters, out _primaryWaits);
        _secondaryFrames   = Array.Empty<int>();
        _secondaryFaces    = Array.Empty<bool>();
        _secondaryCounters = Array.Empty<int>();
        _secondaryWaits    = Array.Empty<int>();
    }

    /// <summary>
    /// Begin a dual-dice animation (attack vs defense): success = primary sixes &gt; secondary sixes.
    /// Either group can be zero; an empty group rolls instantly to "0 sixes".
    /// </summary>
    public void StartDual(int primaryDice, int secondaryDice,
                           string primaryLabel, string secondaryLabel,
                           string? subtitle = null,
                           Vector4? accentColor = null)
    {
        NumberOfDice          = Math.Max(0, primaryDice);
        SecondaryNumberOfDice = Math.Max(0, secondaryDice);
        Difficulty            = 0; // ignored in dual mode
        IsRolling             = true;
        IsVisible             = true;
        IsDual                = true;
        _finalPrimary         = null;
        _finalSecondary       = null;
        ContinueButtonRegion  = default;
        _spinnerFrame         = 0;
        _lastFrameUpdate      = DateTime.MinValue;
        Subtitle              = subtitle;
        DifficultyVerb        = "to hit";
        PrimaryLabel          = primaryLabel;
        SecondaryLabel        = secondaryLabel;
        AccentColor           = accentColor;

        InitArrays(NumberOfDice,          out _primaryFrames,    out _primaryFaces,    out _primaryCounters,    out _primaryWaits);
        InitArrays(SecondaryNumberOfDice, out _secondaryFrames,  out _secondaryFaces,  out _secondaryCounters,  out _secondaryWaits);
    }

    /// <summary>Stop the rolling animation and lock in the single-roll result.</summary>
    public void Complete(int[] finalValues)
    {
        _finalPrimary   = finalValues;
        _finalSecondary = null;
        IsRolling       = false;
    }

    /// <summary>Stop the rolling animation and lock in both dice groups (dual mode).</summary>
    public void CompleteDual(int[] primaryValues, int[] secondaryValues)
    {
        _finalPrimary   = primaryValues;
        _finalSecondary = secondaryValues;
        IsRolling       = false;
    }

    public void Hide()
    {
        IsVisible       = false;
        IsRolling       = false;
        _finalPrimary   = null;
        _finalSecondary = null;
    }

    /// <summary>Advance the rolling animation. Call once per update frame while <see cref="IsRolling"/>.</summary>
    public void Advance()
    {
        if (!IsRolling) return;
        if ((DateTime.Now - _lastFrameUpdate).TotalMilliseconds <= 80) return;

        _lastFrameUpdate = DateTime.Now;
        _spinnerFrame++;

        bool anyChanged = AdvanceGroup(_primaryFrames, _primaryFaces, _primaryCounters, _primaryWaits);
        if (IsDual)
            anyChanged |= AdvanceGroup(_secondaryFrames, _secondaryFaces, _secondaryCounters, _secondaryWaits);

        if (anyChanged) OnDiceTick?.Invoke();
    }

    private bool AdvanceGroup(int[] frames, bool[] faces, int[] counters, int[] waits)
    {
        bool changed = false;
        for (int i = 0; i < frames.Length; i++)
        {
            counters[i]++;
            if (counters[i] < waits[i]) continue;
            counters[i] = 0;
            waits[i]    = _rng.Next(1, 6);
            faces[i]    = !faces[i];
            frames[i]   = faces[i]
                ? _rng.Next(Config.Symbols.DiceFaces.Length)
                : _rng.Next(Config.Symbols.DiceSideViews.Length);
            changed = true;
        }
        return changed;
    }

    // ── Rendering ─────────────────────────────────────────────────────

    /// <summary>
    /// Render the dice roll overlay centered at (<paramref name="centerX"/>, <paramref name="centerY"/>).
    /// Returns <c>true</c> when the "Continue" button is visible (rolling is done).
    /// </summary>
    public bool Render(TerminalHUD terminal, int centerX, int centerY, bool isContinueHovered)
    {
        if (!IsVisible) return false;

        bool usePurple = AccentColor.HasValue;
        Vector4 borderColor    = usePurple ? AccentColor!.Value           : Config.Colors.DarkYellowGrey;
        Vector4 rollingTitle   = usePurple ? Config.Colors.LightPurple    : Config.Colors.Yellow;
        Vector4 successTitle   = usePurple ? Config.Colors.BrightPurple   : Config.Colors.GoldYellow;
        Vector4 failureTitle   = usePurple ? Config.Colors.Purple         : Config.Colors.BrightPurple;
        Vector4 diffLineCol    = usePurple ? Config.Colors.LightPurpleGray : Config.Colors.DarkYellowGrey;
        Vector4 diceRollingCol = usePurple ? Config.Colors.LightPurple    : Config.Colors.Yellow;
        Vector4 diceSixCol     = usePurple ? Config.Colors.BrightPurple   : Config.Colors.GoldYellow;
        Vector4 diceOtherCol   = usePurple ? Config.Colors.LightPurpleGray : Config.Colors.DarkYellowGrey;

        bool hasFinal = !IsRolling && _finalPrimary != null && (!IsDual || _finalSecondary != null);
        int primarySixes   = hasFinal ? _finalPrimary!.Count(v => v == 6) : 0;
        int secondarySixes = hasFinal && IsDual ? _finalSecondary!.Count(v => v == 6) : 0;
        bool isSuccess = IsDual ? primarySixes > secondarySixes : primarySixes >= Difficulty;

        // ── Box sizing — extra rows for the secondary dice group when in dual mode
        int primaryRows   = ((Math.Max(1, NumberOfDice)          + 19) / 20) * 2;
        int secondaryRows = IsDual ? ((Math.Max(1, SecondaryNumberOfDice) + 19) / 20) * 2 + 2 : 0;
        int bgW = 60, bgH = 19 + primaryRows + secondaryRows;
        int bgX = centerX - bgW / 2;
        int bgY = centerY - 14;
        terminal.FillRect(bgX, bgY, bgW, bgH, ' ', Config.Colors.White, Config.Colors.Black);
        terminal.DrawBox(bgX, bgY, bgW, bgH, BoxStyle.Single, borderColor, Config.Colors.Black);

        if (!string.IsNullOrEmpty(Subtitle))
        {
            int subY = centerY - 12;
            int subX = centerX - Subtitle.Length / 2;
            terminal.Text(subX, subY, Subtitle, borderColor, Config.Colors.Black);
        }

        string title = IsRolling ? "Rolling Dice..." : (isSuccess ? "SUCCESS!" : "FAILURE!");
        Vector4 titleCol = IsRolling ? rollingTitle : (isSuccess ? successTitle : failureTitle);
        int titleY = centerY - 10;
        terminal.Text(centerX - title.Length / 2, titleY, title, titleCol, Config.Colors.Black);

        // ── Difficulty / verdict line ───────────────────────────────────
        int diffY = centerY - 8;
        if (IsDual)
        {
            string vsLine = $"{PrimaryLabel} vs {SecondaryLabel}  (more sixes wins)";
            terminal.Text(centerX - vsLine.Length / 2, diffY, vsLine, diffLineCol, Config.Colors.Black);
        }
        else
        {
            int diffClamp = Math.Clamp(Difficulty, 1, 10);
            char diffGlyph = Config.Symbols.DifficultyGlyphs[diffClamp - 1];
            string diffLabel = $"Difficulty: {diffGlyph} ({diffClamp} sixes needed {DifficultyVerb})";
            terminal.Text(centerX - diffLabel.Length / 2, diffY, diffLabel, diffLineCol, Config.Colors.Black);
        }

        // ── Primary dice grid ───────────────────────────────────────────
        int primaryStartY = centerY - 5;
        if (IsDual && !string.IsNullOrEmpty(PrimaryLabel))
        {
            string lbl = $"── {PrimaryLabel} ──";
            terminal.Text(centerX - lbl.Length / 2, primaryStartY - 1, lbl, diffLineCol, Config.Colors.Black);
        }
        RenderDiceGroup(terminal, centerX, primaryStartY,
            NumberOfDice, _primaryFrames, _primaryFaces, _finalPrimary,
            diceRollingCol, diceSixCol, diceOtherCol);

        // ── Secondary dice grid (defense) ───────────────────────────────
        int secondaryStartY = primaryStartY + primaryRows + 1;
        if (IsDual)
        {
            if (!string.IsNullOrEmpty(SecondaryLabel))
            {
                string lbl = $"── {SecondaryLabel} ──";
                terminal.Text(centerX - lbl.Length / 2, secondaryStartY, lbl, diffLineCol, Config.Colors.Black);
            }
            RenderDiceGroup(terminal, centerX, secondaryStartY + 1,
                SecondaryNumberOfDice, _secondaryFrames, _secondaryFaces, _finalSecondary,
                diceRollingCol, diceSixCol, diceOtherCol);
        }

        // ── Result summary or spinner ────────────────────────────────────
        int summaryY = secondaryStartY + (IsDual ? primaryRows : 0) + 2;
        if (!IsDual) summaryY = primaryStartY + primaryRows + 2;

        if (hasFinal)
        {
            string summary;
            if (IsDual)
                summary = $"{PrimaryLabel}: {primarySixes}  |  {SecondaryLabel}: {secondarySixes}";
            else
                summary = $"Rolled {primarySixes} {(primarySixes == 1 ? "six" : "sixes")} out of {NumberOfDice} dice";
            Vector4 sumCol = isSuccess ? successTitle : failureTitle;
            terminal.Text(centerX - summary.Length / 2, summaryY, summary, sumCol, Config.Colors.Black);

            const string btnText = "[ Continue ]";
            int btnX = centerX - btnText.Length / 2;
            int btnY = summaryY + 3;
            var btnFg = isContinueHovered ? Config.Colors.Black : Config.Colors.White;
            var btnBg = isContinueHovered ? Config.Colors.White : Config.Colors.DarkGray;
            terminal.Text(btnX, btnY, btnText, btnFg, btnBg);
            ContinueButtonRegion = (btnX, btnY, btnText.Length);
            return true;
        }
        else
        {
            string spinner = Config.Symbols.LoadingSpinner[_spinnerFrame % Config.Symbols.LoadingSpinner.Length];
            string waitMsg = $"{spinner}  Please wait...  {spinner}";
            terminal.Text(centerX - waitMsg.Length / 2, summaryY, waitMsg, diffLineCol, Config.Colors.Black);
        }

        return false;
    }

    private static void RenderDiceGroup(TerminalHUD terminal, int centerX, int startY,
        int count, int[] frames, bool[] faces, int[]? finalValues,
        Vector4 rollingColor, Vector4 sixColor, Vector4 otherColor)
    {
        if (count <= 0)
        {
            string none = "(none)";
            terminal.Text(centerX - none.Length / 2, startY, none, otherColor, Config.Colors.Black);
            return;
        }
        int dicePerRow = Math.Min(count, 20);
        int spacing    = 2;
        int xStart     = centerX - (dicePerRow * spacing) / 2;
        for (int i = 0; i < count; i++)
        {
            int row = (i / dicePerRow) * 2;
            int col = i % dicePerRow;
            int dx  = xStart + col * spacing + (row % 2);
            int dy  = startY + row;

            char ch;
            Vector4 color;
            if (finalValues != null && i < finalValues.Length)
            {
                int val = Math.Clamp(finalValues[i], 1, 6);
                ch    = Config.Symbols.DiceFaces[val - 1];
                color = val == 6 ? sixColor : otherColor;
            }
            else if (i < frames.Length)
            {
                ch = faces[i]
                    ? Config.Symbols.DiceFaces[frames[i]]
                    : Config.Symbols.DiceSideViews[frames[i]];
                color = rollingColor;
            }
            else
            {
                ch    = Config.Symbols.DiceFaces[0];
                color = otherColor;
            }
            terminal.SetCell(dx, dy, ch, color, Config.Colors.Black);
        }
    }

    private void InitArrays(int n, out int[] frames, out bool[] faces, out int[] counters, out int[] waits)
    {
        frames   = new int[n];
        faces    = new bool[n];
        counters = new int[n];
        waits    = new int[n];
        for (int i = 0; i < n; i++)
        {
            faces[i]    = _rng.Next(2) == 0;
            counters[i] = 0;
            waits[i]    = _rng.Next(1, 6);
            frames[i]   = faces[i]
                ? _rng.Next(Config.Symbols.DiceFaces.Length)
                : _rng.Next(Config.Symbols.DiceSideViews.Length);
        }
    }
}
