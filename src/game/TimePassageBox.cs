using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;
using OpenTK.Mathematics;

namespace Cathedral.Game;

/// <summary>
/// The box that shows time passing in a narration: the six periods of the day in a row, and a marker
/// walking from the hour it was to the hour it is now, one period at a time, wrapping through night
/// into dawn — which is the turn of a day.
///
/// <para>Two ways in. <see cref="ShowPassage"/> plays a passage already decided (a wait that ran its
/// course, an hour wasted on a failure, being led to a trader's premises). <see cref="ShowChoice"/>
/// asks first: every period but the present one is a button, and the passage plays toward the one
/// clicked (sitting and waiting). Either way it ends on a [ Continue ] the player presses.</para>
///
/// <para>Same shape as <see cref="DiceRollComponent"/>, and drawn the same way over the greyed
/// narration: a lifecycle the controller drives (<see cref="Advance"/> every frame,
/// <see cref="Render"/> every frame, <see cref="Hide"/> on Continue) and click regions it hit-tests.
/// The animation is presentation only — the period itself is written elsewhere, by the one writer
/// of it (<c>NarrativeController.ApplyTimePeriod</c>).</para>
/// </summary>
public class TimePassageBox
{
    private static readonly TimePeriod[] Periods = (TimePeriod[])Enum.GetValues(typeof(TimePeriod));

    /// <summary>How long the marker rests on each period it passes through.</summary>
    private const int StepMs = 380;

    // A cell wide enough for the widest button, "[Afternoon]", with a space either side: a button
    // wider than its cell overlaps the next one's, and the click goes to whichever is hit-tested first.
    private const int BoxWidth = 80;
    private const int CellWidth = 12;

    private TimePeriod _from;
    private TimePeriod _to;
    private int        _stepsTotal;
    private int        _stepsShown;
    private DateTime   _lastStep;
    private HashSet<TimePeriod> _choosable = new();
    private string?    _note;

    public bool IsVisible   { get; private set; }
    public bool IsChoosing  { get; private set; }
    public bool IsAnimating => IsVisible && !IsChoosing && _stepsShown < _stepsTotal;
    public bool IsSettled   => IsVisible && !IsChoosing && _stepsShown >= _stepsTotal;

    /// <summary>The period the box is walking toward (or, while choosing, the present one).</summary>
    public TimePeriod Destination => _to;

    /// <summary>Whether the passage crosses from night into dawn: a day turns.</summary>
    public bool TurnsADay => _stepsTotal > 0 && (int)_to <= (int)_from;

    public (int X, int Y, int Width) ContinueButtonRegion { get; private set; }

    /// <summary>The clickable period buttons while choosing; empty otherwise.</summary>
    public IReadOnlyList<(TimePeriod Period, int X, int Y, int Width)> PeriodRegions => _periodRegions;
    private readonly List<(TimePeriod, int, int, int)> _periodRegions = new();

    /// <summary>Raised when the player clicks a period while choosing.</summary>
    public Action<TimePeriod>? OnChosen { get; set; }

    /// <summary>Raised on each step of the marker (a tick sound).</summary>
    public Action? OnTick { get; set; }

    private TimePeriod? _hoveredPeriod;
    private bool _continueHovered;

    // ── Lifecycle ─────────────────────────────────────────────────────

    /// <summary>Plays a passage from <paramref name="from"/> to <paramref name="to"/>.</summary>
    public void ShowPassage(TimePeriod from, TimePeriod to, string? note = null)
    {
        _from       = from;
        _to         = to;
        _stepsTotal = Steps(from, to);
        _stepsShown = 0;
        _lastStep   = DateTime.UtcNow;
        _note       = note;
        IsChoosing  = false;
        IsVisible   = true;
        _periodRegions.Clear();
        ContinueButtonRegion = default;
    }

    /// <summary>Asks which period to wait until; <paramref name="choosable"/> are the buttons.</summary>
    public void ShowChoice(TimePeriod from, IEnumerable<TimePeriod> choosable)
    {
        _from       = from;
        _to         = from;
        _stepsTotal = 0;
        _stepsShown = 0;
        _note       = null;
        _choosable  = choosable.ToHashSet();
        IsChoosing  = true;
        IsVisible   = true;
        ContinueButtonRegion = default;
    }

    /// <summary>Picks <paramref name="period"/> as if clicked. False when it is not on offer.</summary>
    public bool Choose(TimePeriod period)
    {
        if (!IsChoosing || !_choosable.Contains(period)) return false;
        OnChosen?.Invoke(period);
        return true;
    }

    public void Hide()
    {
        IsVisible  = false;
        IsChoosing = false;
        _periodRegions.Clear();
        ContinueButtonRegion = default;
        _hoveredPeriod   = null;
        _continueHovered = false;
    }

    /// <summary>Moves the marker on when its step has elapsed. Call every update frame.</summary>
    public void Advance()
    {
        if (!IsAnimating) return;
        if ((DateTime.UtcNow - _lastStep).TotalMilliseconds < StepMs) return;
        _lastStep = DateTime.UtcNow;
        _stepsShown++;
        OnTick?.Invoke();
    }

    /// <summary>How many periods forward <paramref name="to"/> lies from <paramref name="from"/>, wrapping.</summary>
    public static int Steps(TimePeriod from, TimePeriod to)
        => (((int)to - (int)from) % Periods.Length + Periods.Length) % Periods.Length;

    // ── Input ─────────────────────────────────────────────────────────

    /// <summary>Tracks hover; returns true when the hovered element changed (for a hover sound).</summary>
    public bool HandleHover(int x, int y)
    {
        var before = (_hoveredPeriod, _continueHovered);
        _hoveredPeriod = null;
        foreach (var (p, rx, ry, rw) in _periodRegions)
            if (y == ry && x >= rx && x < rx + rw) _hoveredPeriod = p;
        var c = ContinueButtonRegion;
        _continueHovered = c.Width > 0 && y == c.Y && x >= c.X && x < c.X + c.Width;
        return before != (_hoveredPeriod, _continueHovered) && (_hoveredPeriod != null || _continueHovered);
    }

    /// <summary>A click on a period button while choosing. True when it chose one.</summary>
    public bool HandlePeriodClick(int x, int y)
    {
        if (!IsChoosing) return false;
        foreach (var (p, rx, ry, rw) in _periodRegions)
            if (y == ry && x >= rx && x < rx + rw) return Choose(p);
        return false;
    }

    /// <summary>Whether (x, y) is on the [ Continue ] button.</summary>
    public bool IsOnContinue(int x, int y)
    {
        var c = ContinueButtonRegion;
        return IsSettled && c.Width > 0 && y == c.Y && x >= c.X && x < c.X + c.Width;
    }

    // ── Rendering ─────────────────────────────────────────────────────

    /// <summary>Draws the box centred at (<paramref name="centerX"/>, <paramref name="centerY"/>).</summary>
    public void Render(TerminalHUD terminal, int centerX, int centerY)
    {
        if (!IsVisible) return;

        Vector4 border  = Config.Colors.LightGray;
        Vector4 bg      = Config.Colors.Black;
        Vector4 neutral = Config.Colors.Gray;

        int bgW = BoxWidth, bgH = 13;
        int bgX = centerX - bgW / 2;
        int bgY = centerY - bgH / 2;
        terminal.FillRect(bgX, bgY, bgW, bgH, ' ', Config.Colors.White, bg);
        terminal.DrawBox(bgX, bgY, bgW, bgH, BoxStyle.Single, border, bg);

        string title = IsChoosing ? "WAIT UNTIL..." : "TIME PASSES";
        terminal.Text(centerX - title.Length / 2, bgY + 2, title, IsChoosing ? Config.Colors.White : Config.Colors.Yellow, bg);

        // The marker's position: where the walk has reached.
        var at = _from.Advance(_stepsShown);

        // The six periods. Passed ones grey, the marker yellow, the rest dim; while choosing, every
        // period on offer is a button.
        _periodRegions.Clear();
        int rowY   = bgY + 5;
        int startX = centerX - Periods.Length * CellWidth / 2;
        for (int i = 0; i < Periods.Length; i++)
        {
            var p     = Periods[i];
            string lb = p.Label();
            int cx    = startX + i * CellWidth;
            int lx    = cx + (CellWidth - lb.Length) / 2;

            if (IsChoosing && _choosable.Contains(p))
            {
                bool hov = _hoveredPeriod == p;
                string btn = $"[{lb}]";
                int bx = cx + (CellWidth - btn.Length) / 2;
                terminal.Text(bx, rowY, btn, hov ? Config.Colors.Black : Config.Colors.White,
                              hov ? Config.Colors.White : Config.Colors.DarkGray);
                _periodRegions.Add((p, bx, rowY, btn.Length));
                continue;
            }

            Vector4 fg = p == at                  ? Config.Colors.Yellow
                       : IsPassed(p)              ? Config.Colors.LightGray
                       : p == _to && !IsChoosing  ? Config.Colors.White
                       :                            Config.Colors.DarkGray;
            terminal.Text(lx, rowY, lb, fg, bg);
            if (p == at)
                terminal.SetCell(cx + CellWidth / 2, rowY + 1, '^', Config.Colors.Yellow, bg);
        }

        // The line under the row: what is being asked, or where the walk is going.
        string line = IsChoosing
            ? $"It is {_from.Label().ToLowerInvariant()}. Choose the hour to wait until."
            : _stepsTotal == 0 ? $"It is still {_to.Label().ToLowerInvariant()}."
            : $"{_from.Label()}  ->  {_to.Label()}" + (TurnsADay ? "   (a new day)" : "");
        terminal.Text(centerX - line.Length / 2, bgY + 8, line, neutral, bg);
        if (_note != null)
            terminal.Text(centerX - _note.Length / 2, bgY + 9, _note, Config.Colors.Purple, bg);

        if (IsSettled)
        {
            const string btnText = "[ Continue ]";
            int btnX = centerX - btnText.Length / 2;
            int btnY = bgY + 11;
            terminal.Text(btnX, btnY, btnText,
                _continueHovered ? Config.Colors.Black : Config.Colors.White,
                _continueHovered ? Config.Colors.White : Config.Colors.DarkGray);
            ContinueButtonRegion = (btnX, btnY, btnText.Length);
        }
        else
        {
            ContinueButtonRegion = default;
        }
    }

    /// <summary>Whether the marker has already walked past <paramref name="p"/> on this passage.</summary>
    private bool IsPassed(TimePeriod p)
    {
        for (int s = 0; s < _stepsShown; s++)
            if (_from.Advance(s) == p) return true;
        return false;
    }
}
