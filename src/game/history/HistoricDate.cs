using System;

namespace Cathedral.Game.History;

/// <summary>How much a date is worth: known to the round, known roughly, or not known at all.</summary>
public enum DatePrecision
{
    Exact,
    Circa,
    Unknown,
}

/// <summary>
/// A point in history, in <b>rounds</b> (360 days) counted from the Foundation at Dokur: the lore's
/// FC count, negative before it. See <c>lore/03_world_science.md</c>, "Reckoning time".
///
/// <para>A round is the only resolution history works at. The game's own clock counts days from the
/// start of a run and never meets this one; the run's first day falls inside
/// <see cref="HistoryCalendar.PresentRound"/>.</para>
///
/// <para><see cref="DatePrecision.Unknown"/> is a real value and not a failure. A figure created on the
/// fly while the simulation stands at some round (a realm's founder, say) was born before that round,
/// and the chronology cannot record an event in its own past, so the birth stays unknown. Everything
/// that orders or compares dates treats an unknown date as "no information", never as zero.</para>
/// </summary>
public readonly record struct HistoricDate(int Round, DatePrecision Precision)
{
    public static HistoricDate Unknown => new(0, DatePrecision.Unknown);

    public static HistoricDate At(int round) => new(round, DatePrecision.Exact);

    public static HistoricDate Circa(int round) => new(round, DatePrecision.Circa);

    /// <summary>A date counted After the Hatching, as post-imperial chronicles count.</summary>
    public static HistoricDate AH(int roundsAfterHatching)
        => At(HistoryCalendar.HatchingRound + roundsAfterHatching);

    public bool IsKnown => Precision != DatePrecision.Unknown;

    /// <summary>True when both dates are known and this one is strictly earlier.</summary>
    public bool IsBefore(HistoricDate other) => IsKnown && other.IsKnown && Round < other.Round;

    /// <summary>
    /// "1703 FC", "c. -1600 FC", "612 AH" (after the Hatching, the count every surviving world
    /// uses), or "date unknown".
    /// </summary>
    public override string ToString()
    {
        if (!IsKnown) return "date unknown";
        string circa = Precision == DatePrecision.Circa ? "c. " : "";
        return Round > HistoryCalendar.HatchingRound
            ? $"{circa}{Round - HistoryCalendar.HatchingRound} AH"
            : $"{circa}{Round} FC";
    }
}

/// <summary>
/// The fixed points every history hangs off. All in FC rounds.
///
/// <para>The present is one date for every world, by design: every world is played at the same
/// moment of the cosmos, so the post-imperial lore any world tells is the same age everywhere.</para>
/// </summary>
public static class HistoryCalendar
{
    /// <summary>The capitulation of Dokur: the birth of the half-elf empire.</summary>
    public const int FoundationRound = 0;

    /// <summary>The hatching of Pyr, and the end of the empire. 0 AH.</summary>
    public const int HatchingRound = 3579;

    /// <summary>
    /// "Now": the round the game is played in, 612 AH. The lore leaves the present open by a century
    /// either way; this is the value the generator settles it to.
    /// </summary>
    public const int PresentRound = HatchingRound + 612;

    /// <summary>
    /// Where a generated world's own history begins: about when the Ban were drinking their first
    /// blood (the Delivery, -1052). Before this a world simply has what it starts with: a few
    /// ancient faiths, and land waiting for its first realms. Set from -2000 to -1000 to keep a
    /// world's past readable; every round of it is another generation of rulers.
    /// </summary>
    public const int LocalHistoryStart = -1000;

    /// <summary>Round of the founding of the Principian Inquisition, after which a held world is policed.</summary>
    public const int InquisitionRound = 2533;

    /// <summary>The start and practical end of the second expansion: when most held worlds were taken.</summary>
    public const int SecondExpansionStart = 2540;
    public const int SecondExpansionEnd = 3100;

    /// <summary>Clamps a round into the span a world's history is simulated over.</summary>
    public static int ClampToLocal(int round)
        => Math.Clamp(round, LocalHistoryStart, PresentRound);
}
