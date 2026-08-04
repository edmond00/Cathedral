using System;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;
using Cathedral.Game.Narrative;

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

    // Drives the tumbling faces during the animation, not the result — but seeded all the same, so
    // that a replayed run is identical frame for frame rather than merely identical in outcome.
    private readonly Random _rng = GameRng.Stream("dice-animation");

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

    /// <summary>
    /// Fires once when the rolling animation ends and the result is locked in (via
    /// <see cref="Complete"/> or <see cref="CompleteDual"/>). The bool is <c>true</c> when
    /// the roll succeeded (single-roll: sixes ≥ Difficulty; dual-roll: primary sixes &gt; secondary sixes).
    /// Reassign before every roll so the callback reflects the current context.
    /// </summary>
    public Action<bool>? OnResultRevealed { get; set; }

    private int[]? _finalPrimary;
    private int[]? _finalSecondary;

    // ── Humor modifier layer ──────────────────────────────────────────
    // Opt-in per roll via EnableHumorModifiers(). When disabled the component behaves exactly
    // as before. The player may spend the tail humor of each of their 4 organ queues to modify
    // individual dice in the PRIMARY group, up to a per-roll limit.
    private HumorQueueSet? _humorQueues;
    private int  _humorLimit;
    private int  _humorApplied;
    private bool _humorEnabled;

    private int _selectedQueue = -1;   // 0..3, index into HumorQueuesOrdered; -1 = none selected
    private int _hoveredQueue  = -1;   // button under the cursor; -1 = none
    private int _hoveredDie    = -1;   // primary die under the cursor; -1 = none

    private readonly Random _humorRng = GameRng.Stream("dice-humor");
    private readonly (int X, int Y, int Width)[] _humorButtons = new (int, int, int)[4];
    private (int X, int Y)[] _primaryDiceCells = Array.Empty<(int, int)>();

    /// <summary>The (possibly humor-modified) final values of the primary dice group.</summary>
    public int[] FinalPrimaryValues => _finalPrimary ?? Array.Empty<int>();

    /// <summary>True when humor modifiers are active for the current roll.</summary>
    public bool HumorEnabled => _humorEnabled;

    /// <summary>
    /// Live success state, recomputed after every humor modifier
    /// (single: primary sixes ≥ Difficulty; dual: primary sixes &gt; secondary sixes).
    /// </summary>
    public bool IsCurrentlySuccess { get; private set; }

    /// <summary>Fires when a humor button is first hovered — hook a short tick.</summary>
    public Action? OnButtonHover { get; set; }

    /// <summary>Fires when a humor button or a die is clicked — hook a click sound.</summary>
    public Action? OnButtonClick { get; set; }

    /// <summary>
    /// Fires when applying a humor modifier flips the success state. The bool is the new
    /// success value. Use it to swap pre-generated narration and replay the outcome sound.
    /// </summary>
    public Action<bool>? OnResultChanged { get; set; }

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

        ResetHumorState();
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

        ResetHumorState();
        InitArrays(NumberOfDice,          out _primaryFrames,    out _primaryFaces,    out _primaryCounters,    out _primaryWaits);
        InitArrays(SecondaryNumberOfDice, out _secondaryFrames,  out _secondaryFaces,  out _secondaryCounters,  out _secondaryWaits);
    }

    /// <summary>
    /// Enable the humor-modifier layer for the current roll. Call right after
    /// <see cref="Start"/> / <see cref="StartDual"/>. <paramref name="queues"/> is the acting
    /// player's organ queues; <paramref name="modifierLimit"/> caps how many tail humors may be
    /// spent on this roll (the viscera <c>humor_modifier_limit</c> derived stat).
    /// Only the player's own (primary) dice group is ever modifiable.
    /// </summary>
    public void EnableHumorModifiers(HumorQueueSet queues, int modifierLimit)
    {
        _humorQueues  = queues;
        _humorLimit   = Math.Max(0, modifierLimit);
        _humorApplied = 0;
        _humorEnabled = _humorLimit > 0;
        _selectedQueue = -1;
        _hoveredQueue  = -1;
        _hoveredDie    = -1;
    }

    private void ResetHumorState()
    {
        _humorQueues   = null;
        _humorEnabled  = false;
        _humorLimit    = 0;
        _humorApplied  = 0;
        _selectedQueue = -1;
        _hoveredQueue  = -1;
        _hoveredDie    = -1;
        _primaryDiceCells = Array.Empty<(int, int)>();
        for (int i = 0; i < _humorButtons.Length; i++) _humorButtons[i] = default;
    }

    /// <summary>The four organ queues in button display order: Stomach, Hepar, Spleen, Pulmones.</summary>
    private HumorQueue[] HumorQueuesOrdered()
        => _humorQueues == null
            ? Array.Empty<HumorQueue>()
            : new[] { _humorQueues.Paunch, _humorQueues.Hepar, _humorQueues.Spleen, _humorQueues.Pulmones };

    /// <summary>Stop the rolling animation and lock in the single-roll result.</summary>
    public void Complete(int[] finalValues)
    {
        _finalPrimary   = finalValues;
        _finalSecondary = null;
        IsRolling       = false;
        IsCurrentlySuccess = ComputeSuccess();
        OnResultRevealed?.Invoke(IsCurrentlySuccess);
    }

    /// <summary>Stop the rolling animation and lock in both dice groups (dual mode).</summary>
    public void CompleteDual(int[] primaryValues, int[] secondaryValues)
    {
        _finalPrimary   = primaryValues;
        _finalSecondary = secondaryValues;
        IsRolling       = false;
        IsCurrentlySuccess = ComputeSuccess();
        OnResultRevealed?.Invoke(IsCurrentlySuccess);
    }

    /// <summary>Recompute the success state from the current (possibly modified) final dice.</summary>
    private bool ComputeSuccess()
    {
        if (_finalPrimary == null) return false;
        int primarySixes = _finalPrimary.Count(v => v == 6);
        if (IsDual)
        {
            int secondarySixes = _finalSecondary?.Count(v => v == 6) ?? 0;
            return primarySixes > secondarySixes;
        }
        return primarySixes >= Difficulty;
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

        // Unified grayscale/white scheme (roll type no longer changes the palette):
        //   • neutral text/border in grays/white   • 6s in yellow, other dice in gray
        //   • success messages yellow, failure messages purple
        Vector4 borderColor    = Config.Colors.LightGray;
        Vector4 rollingTitle   = Config.Colors.White;
        Vector4 successTitle   = Config.Colors.Yellow;   // success-related messages
        Vector4 failureTitle   = Config.Colors.Purple;   // failure-related messages
        Vector4 diffLineCol    = Config.Colors.Gray;     // neutral labels / subtitle
        Vector4 diceRollingCol = Config.Colors.LightGray;
        Vector4 diceSixCol     = Config.Colors.Yellow;   // a rolled 6
        Vector4 diceOtherCol   = Config.Colors.Gray;     // any other face

        bool hasFinal = !IsRolling && _finalPrimary != null && (!IsDual || _finalSecondary != null);
        int primarySixes   = hasFinal ? _finalPrimary!.Count(v => v == 6) : 0;
        int secondarySixes = hasFinal && IsDual ? _finalSecondary!.Count(v => v == 6) : 0;
        // The verdict shown is the verdict committed: read IsCurrentlySuccess rather than
        // recomputing it here. A second local computation can drift from the one the caller acts
        // on (it did), leaving the player told SUCCESS while the failure branch is applied.
        bool isSuccess = hasFinal && IsCurrentlySuccess;

        // ── Box sizing — extra rows for the secondary dice group when in dual mode
        int primaryRows   = ((Math.Max(1, NumberOfDice)          + 19) / 20) * 2;
        int secondaryRows = IsDual ? ((Math.Max(1, SecondaryNumberOfDice) + 19) / 20) * 2 + 2 : 0;
        int humorRows     = _humorEnabled ? 3 : 0; // button row + hint row + padding
        int bgW = 60, bgH = 19 + primaryRows + secondaryRows + humorRows;
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
            // The glyph keeps its difficulty-gradient color; the rest of the line is neutral gray.
            string prefix = "Difficulty: ";
            string suffix = $" ({diffClamp} sixes needed {DifficultyVerb})";
            int diffX = centerX - (prefix.Length + 1 + suffix.Length) / 2;
            terminal.Text(diffX, diffY, prefix, diffLineCol, Config.Colors.Black);
            terminal.SetCell(diffX + prefix.Length, diffY, diffGlyph,
                Config.Symbols.DifficultyLevelColor(diffClamp), Config.Colors.Black);
            terminal.Text(diffX + prefix.Length + 1, diffY, suffix, diffLineCol, Config.Colors.Black);
        }

        // ── Primary dice grid ───────────────────────────────────────────
        int primaryStartY = centerY - 5;
        if (IsDual && !string.IsNullOrEmpty(PrimaryLabel))
        {
            string lbl = $"── {PrimaryLabel} ──";
            terminal.Text(centerX - lbl.Length / 2, primaryStartY - 1, lbl, diffLineCol, Config.Colors.Black);
        }
        if (hasFinal && _humorEnabled)
            RenderPrimaryDiceInteractive(terminal, centerX, primaryStartY, diceSixCol, diceOtherCol);
        else
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

            if (_humorEnabled)
                RenderHumorButtons(terminal, centerX, btnY + 2);

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

    // ── Humor modifier rendering ───────────────────────────────────────

    /// <summary>
    /// Render the primary (player) dice once settled, colouring them by humor-selection state and
    /// capturing each die's cell so clicks can be hit-tested. Greys out dice the selected humor
    /// cannot modify; highlights the hovered clickable die.
    /// </summary>
    private void RenderPrimaryDiceInteractive(TerminalHUD terminal, int centerX, int startY,
        Vector4 sixColor, Vector4 otherColor)
    {
        int count = NumberOfDice;
        if (_finalPrimary == null || count <= 0)
        {
            _primaryDiceCells = Array.Empty<(int, int)>();
            return;
        }
        if (_primaryDiceCells.Length != count) _primaryDiceCells = new (int, int)[count];

        int dicePerRow = Math.Min(count, 20);
        int spacing    = 2;
        int xStart     = centerX - (dicePerRow * spacing) / 2;

        TransmutingVirtue? virtue = _selectedQueue >= 0
            ? HumorQueuesOrdered()[_selectedQueue].PeekConsumable()?.TransmutingVirtue
            : null;
        bool canApplyMore = _humorApplied < _humorLimit;

        for (int i = 0; i < count; i++)
        {
            int row = (i / dicePerRow) * 2;
            int col = i % dicePerRow;
            int dx  = xStart + col * spacing + (row % 2);
            int dy  = startY + row;
            _primaryDiceCells[i] = (dx, dy);

            int val = Math.Clamp(_finalPrimary[i], 1, 6);
            char ch = Config.Symbols.DiceFaces[val - 1];

            Vector4 color = (val == 6) ? sixColor : otherColor;
            Vector4 bg    = Config.Colors.Black;

            if (virtue != null)
            {
                bool clickable = canApplyMore && virtue.CanApplyTo(val);
                if (clickable)
                {
                    if (_hoveredDie == i) { color = Config.Colors.Black; bg = Config.Colors.White; }
                    else                  { color = Config.Colors.White; }
                }
                else
                {
                    color = Config.Colors.DarkGray; // not modifiable by the selected humor
                }
            }

            terminal.SetCell(dx, dy, ch, color, bg);
        }
    }

    /// <summary>
    /// Render the four humor buttons (Stomach, Hepar, Spleen, Pulmones) plus a hint line showing
    /// the hovered/selected humor's transmuting formula. Captures each button's hit region.
    /// </summary>
    private void RenderHumorButtons(TerminalHUD terminal, int centerX, int rowY)
    {
        var queues = HumorQueuesOrdered();
        if (queues.Length == 0) return;

        const int btnW = 5, gap = 3;
        int totalW = queues.Length * btnW + (queues.Length - 1) * gap;
        int startX = centerX - totalW / 2;
        bool limitReached = _humorApplied >= _humorLimit;

        for (int i = 0; i < queues.Length; i++)
        {
            int bx = startX + i * (btnW + gap);
            var humor = queues[i].PeekConsumable();
            bool selectable = !limitReached && humor != null && IsUsableVirtue(humor.TransmutingVirtue);
            bool selected   = _selectedQueue == i;
            bool hovered    = _hoveredQueue  == i;

            // Fall back to the (black-bile) tail glyph when the queue is fully critical.
            char glyph        = humor?.Symbol ?? queues[i].Items[HumorQueue.Capacity - 1].Symbol;
            Vector4 humorCol  = humor?.Color ?? Config.Colors.DarkGray;
            Vector4 humorBg   = humor?.BackgroundColor ?? Config.Colors.Black;

            // Brackets are always dark gray so they read as a neutral frame around the humor glyph.
            Vector4 bracket = Config.Colors.DarkGray;
            Vector4 fg, bg;
            if (!selectable)        { fg = Config.Colors.DarkGray; bg = Config.Colors.Black;    }
            else if (selected)      { fg = Config.Colors.Black;    bg = humorCol;               }
            else if (hovered)       { fg = humorCol;               bg = Config.Colors.DarkGray; }
            else                    { fg = humorCol;               bg = humorBg;                }

            terminal.SetCell(bx,     rowY, '[',   bracket, Config.Colors.Black);
            terminal.SetCell(bx + 1, rowY, ' ',   fg, bg);
            terminal.SetCell(bx + 2, rowY, glyph, fg, bg);
            terminal.SetCell(bx + 3, rowY, ' ',   fg, bg);
            terminal.SetCell(bx + 4, rowY, ']',   bracket, Config.Colors.Black);

            _humorButtons[i] = (bx, rowY, btnW);
        }

        // Hint / status line: show the focused humor's formula, else the modifier budget.
        int focus = _hoveredQueue >= 0 ? _hoveredQueue : _selectedQueue;
        string hint;
        if (focus >= 0 && focus < queues.Length
            && queues[focus].PeekConsumable() is BodyHumor fh && fh.TransmutingVirtue != null)
            hint = $"{fh.Name}:  {fh.TransmutingVirtue.Description}    ({_humorApplied}/{_humorLimit})";
        else
            hint = $"Humor modifiers: {_humorApplied}/{_humorLimit}  —  hover a humor to inspect";
        terminal.Text(centerX - hint.Length / 2, rowY + 1, hint,
            Config.Colors.DarkGray, Config.Colors.Black);
    }

    private static bool IsUsableVirtue(TransmutingVirtue? v) => v != null && v is not NullVirtue;

    // ── Humor modifier interaction ─────────────────────────────────────

    /// <summary>
    /// Update humor hover state from a mouse position (terminal cell coords). Returns true when
    /// the cursor is over a humor button or a clickable die. Fires <see cref="OnButtonHover"/>
    /// when newly entering a hoverable element.
    /// </summary>
    public bool HandleHumorHover(int x, int y)
    {
        if (!_humorEnabled || IsRolling || !IsVisible) return false;

        int newQueue = HitTestButton(x, y);
        int rawDie   = HitTestDie(x, y);
        int newDie   = (rawDie >= 0 && IsDieClickable(rawDie)) ? rawDie : -1;

        if (newQueue != _hoveredQueue)
        {
            if (newQueue >= 0) OnButtonHover?.Invoke();
            _hoveredQueue = newQueue;
        }
        if (newDie != _hoveredDie)
        {
            if (newDie >= 0) OnButtonHover?.Invoke();
            _hoveredDie = newDie;
        }
        return newQueue >= 0 || newDie >= 0;
    }

    /// <summary>
    /// Handle a click for the humor layer (terminal cell coords). Callers should test the
    /// Continue button FIRST; only forward here if Continue was not clicked. Returns true when
    /// the click was consumed by the humor layer.
    /// </summary>
    public bool HandleHumorClick(int x, int y)
    {
        if (!_humorEnabled || IsRolling || !IsVisible) return false;

        int q = HitTestButton(x, y);
        if (q >= 0)
        {
            var humor = HumorQueuesOrdered()[q].PeekConsumable();
            bool selectable = _humorApplied < _humorLimit && humor != null && IsUsableVirtue(humor.TransmutingVirtue);
            if (selectable)
            {
                _selectedQueue = (_selectedQueue == q) ? -1 : q; // toggle
                _hoveredDie = -1;
                OnButtonClick?.Invoke();
            }
            return true;
        }

        if (_selectedQueue >= 0)
        {
            int die = HitTestDie(x, y);
            if (die >= 0 && IsDieClickable(die))
            {
                ApplyModifierToDie(die);
            }
            else
            {
                // Clicking empty space (or a non-modifiable die) deselects the humor.
                _selectedQueue = -1;
                _hoveredDie = -1;
                OnButtonClick?.Invoke();
            }
            return true;
        }

        return false;
    }

    private void ApplyModifierToDie(int dieIndex)
    {
        if (_finalPrimary == null || dieIndex < 0 || dieIndex >= _finalPrimary.Length) return;
        if (_selectedQueue < 0 || _humorApplied >= _humorLimit) return;

        var queue  = HumorQueuesOrdered()[_selectedQueue];
        var humor  = queue.PeekConsumable();
        var virtue = humor?.TransmutingVirtue;
        if (humor == null || virtue == null || !virtue.CanApplyTo(_finalPrimary[dieIndex])) return;

        bool before = IsCurrentlySuccess;

        // The die's new face and the verdict move together, ahead of everything that could throw
        // (queue bookkeeping, sound callbacks). Updating the verdict last meant any failure in
        // between left a modified die on screen and a stale IsCurrentlySuccess for the caller.
        _finalPrimary[dieIndex] = Math.Clamp(virtue.Apply(_finalPrimary[dieIndex], _humorRng), 1, 6);
        IsCurrentlySuccess = ComputeSuccess();

        queue.ConsumeTailModifier();
        _humorApplied++;

        // Applying a modifier always clears the selection — the player re-picks a humor
        // (the same queue's new tail, or another organ) for the next modification.
        _selectedQueue = -1;
        _hoveredDie = -1;

        OnButtonClick?.Invoke();
        if (IsCurrentlySuccess != before)
            OnResultChanged?.Invoke(IsCurrentlySuccess);
    }

    private int HitTestButton(int x, int y)
    {
        for (int i = 0; i < _humorButtons.Length; i++)
        {
            var (bx, by, bw) = _humorButtons[i];
            if (bw > 0 && y == by && x >= bx && x < bx + bw) return i;
        }
        return -1;
    }

    private int HitTestDie(int x, int y)
    {
        for (int i = 0; i < _primaryDiceCells.Length; i++)
            if (_primaryDiceCells[i].X == x && _primaryDiceCells[i].Y == y) return i;
        return -1;
    }

    private bool IsDieClickable(int dieIndex)
    {
        if (_selectedQueue < 0 || _finalPrimary == null) return false;
        if (_humorApplied >= _humorLimit) return false;
        if (dieIndex < 0 || dieIndex >= _finalPrimary.Length) return false;
        var virtue = HumorQueuesOrdered()[_selectedQueue].PeekConsumable()?.TransmutingVirtue;
        return virtue != null && virtue.CanApplyTo(_finalPrimary[dieIndex]);
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
