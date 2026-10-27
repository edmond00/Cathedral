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

    // A cell wide enough for the widest label, "Afternoon", padded, plus the one-column gap between
    // cells: a button wider than its cell overlaps the next one's, and the click goes to whichever is
    // hit-tested first.
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

    // The travel plan's palette: a dull gold frame, a bright yellow title, grey prose, and solid
    // buttons — black on yellow for the act, light on dark grey for a choice.
    private static Vector4 Bg         => Config.Colors.Black;
    private static Vector4 Frame      => Config.TravelUI.BorderColor;
    private static Vector4 TitleInk   => Config.TravelUI.TitleColor;
    private static Vector4 ProseInk   => Config.Colors.LightGray75;
    private static Vector4 NoteInk    => Config.TravelUI.DangerColor;
    private static Vector4 TrackInk   => Config.Colors.DarkGray35;

    // The strip of periods: each a shaded cell, so the row reads as one dial rather than six words.
    private static readonly Vector4 NowFill     = new(0.130f, 0.105f, 0.015f, 1f); // the location map's "here"
    private static readonly Vector4 PassedFill  = new(0.075f, 0.075f, 0.080f, 1f);
    private static readonly Vector4 AheadFill   = new(0.040f, 0.040f, 0.045f, 1f);

    /// <summary>Draws the box centred at (<paramref name="centerX"/>, <paramref name="centerY"/>).</summary>
    public void Render(TerminalHUD terminal, int centerX, int centerY)
    {
        if (!IsVisible) return;

        // Asking has no note and no Continue, so its box ends under the line; a passage keeps its full
        // height throughout, so the box does not grow when its Continue appears.
        int bgW = BoxWidth, bgH = IsChoosing ? 8 : 12;
        int bgX = centerX - bgW / 2;
        int bgY = centerY - bgH / 2;
        terminal.FillRect(bgX, bgY, bgW, bgH, ' ', ProseInk, Bg);
        terminal.DrawBox(bgX, bgY, bgW, bgH, BoxStyle.Single, Frame, Bg);

        string title = IsChoosing ? "── WAIT UNTIL ──" : "── TIME PASSES ──";
        terminal.Text(centerX - title.Length / 2, bgY + 1, title, TitleInk, Bg);

        // The marker's position: where the walk has reached.
        var at = _from.Advance(_stepsShown);

        // The six periods, one shaded cell each over a track, the marker's cell lit and a ▲ under it.
        // While choosing, every period on offer is a button.
        _periodRegions.Clear();
        int rowY   = bgY + 3;
        int startX = centerX - Periods.Length * CellWidth / 2;
        int cellW  = CellWidth - 1;   // a one-column gap between cells
        for (int x = startX; x < startX + Periods.Length * CellWidth - 1; x++)
            terminal.SetCell(x, rowY + 1, '─', TrackInk, Bg);

        for (int i = 0; i < Periods.Length; i++)
        {
            var p     = Periods[i];
            string lb = p.Label();
            int cx    = startX + i * CellWidth;
            string cell = lb.PadLeft((cellW + lb.Length) / 2).PadRight(cellW);

            Vector4 fg, fill;
            if (IsChoosing && _choosable.Contains(p))
            {
                bool hov = _hoveredPeriod == p;
                fg   = hov ? Config.TravelUI.TravelButtonTextColor       : Config.TravelUI.ClearButtonTextColor;
                fill = hov ? Config.TravelUI.TravelButtonBackgroundColor : Config.TravelUI.ClearButtonBackgroundColor;
                _periodRegions.Add((p, cx, rowY, cellW));
            }
            else if (p == at)
            {
                // Choosing: the hour it is now, which cannot be waited until. Passing: the marker.
                fg   = IsChoosing ? Config.Colors.BrightYellow : Config.TravelUI.TravelButtonTextColor;
                fill = IsChoosing ? NowFill                    : Config.TravelUI.TravelButtonBackgroundColor;
            }
            else if (IsPassed(p)) { fg = Config.Colors.MediumGray50; fill = PassedFill; }
            else if (p == _to)    { fg = Config.Colors.White;        fill = Config.TravelUI.ClearButtonBackgroundColor; }
            else                  { fg = Config.Colors.DarkGray40;   fill = AheadFill; }

            terminal.Text(cx, rowY, cell, fg, fill);
            if (p == at)
                terminal.SetCell(cx + cellW / 2, rowY + 1, '▲', Config.Colors.BrightYellow, Bg);
        }

        // The line under the strip: what is being asked, or where the walk is going.
        string line = IsChoosing
            ? $"It is {_from.Label().ToLowerInvariant()}. Choose the hour to wait until."
            : _stepsTotal == 0 ? $"It is still {_to.Label().ToLowerInvariant()}."
            : $"{_from.Label()}  →  {_to.Label()}" + (TurnsADay ? "   · a new day" : "");
        terminal.Text(centerX - line.Length / 2, bgY + 6, line, ProseInk, Bg);
        if (_note != null)
            terminal.Text(centerX - _note.Length / 2, bgY + 7, _note, NoteInk, Bg);

        if (IsSettled)
        {
            const string btnText = "[ Continue ]";
            int btnX = centerX - btnText.Length / 2;
            int btnY = bgY + 9;
            terminal.Text(btnX, btnY, btnText,
                _continueHovered ? Config.TravelUI.TravelButtonHoverTextColor       : Config.TravelUI.TravelButtonTextColor,
                _continueHovered ? Config.TravelUI.TravelButtonHoverBackgroundColor : Config.TravelUI.TravelButtonBackgroundColor);
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
