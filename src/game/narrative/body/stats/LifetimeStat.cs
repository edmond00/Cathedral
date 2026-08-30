namespace Cathedral.Game.Narrative;

/// <summary>
/// Lifetime — the total span of days a body is granted before it gives out. Derived from the heart,
/// "the index of the constitution's warmth, its span of years" (see <see cref="HeartOrgan"/>).
///
/// <para>
/// A member dies of old age once their age (<see cref="PartyMember.GetAgeDays"/>) reaches this value.
/// The span runs from <see cref="MinLifetimeDays"/> at heart score 0 up to <see cref="MaxLifetimeDays"/>
/// when the heart is at its species maximum, scaled linearly in between. Scaling against the
/// member's own heart max (rather than a hard-coded 5) keeps the range correct for species whose
/// heart tops out elsewhere, still yielding the same 120-year ceiling.
/// </para>
///
/// <para>
/// <b>Wounds shorten life.</b> This stat deliberately goes through the wound-aware path: a
/// medium-handicap heart wound lowers the effective score and takes real days off the member's life,
/// and a high-handicap wound disables the heart entirely, collapsing the lifetime to
/// <see cref="MinLifetimeDays"/> — which will kill outright anyone already older than that. A wound
/// to the heart is meant to be a sentence, not a scratch.
/// </para>
/// </summary>
public sealed class LifetimeStat : DerivedStat
{
    /// <summary>Days in one year of the world's calendar. The day is the only unit the game models;
    /// this constant exists solely to express the design's lifespan figures.</summary>
    public const int DaysPerYear = 360;

    /// <summary>Shortest possible life: 30 years.</summary>
    public const int MinLifetimeDays = 30 * DaysPerYear;   // 10800

    /// <summary>Longest possible life: 120 years.</summary>
    public const int MaxLifetimeDays = 120 * DaysPerYear;  // 43200

    public override string Name            => "lifetime";
    public override string DisplayName     => "Lifetime";
    public override string ShortDisplayName => "Lifetime";
    public override string? RelatedOrganId => "heart";

    public override int WorstValue  => MinLifetimeDays;
    public override int? BestValue  => MaxLifetimeDays;

    // Member-agnostic fallback: assume the human heart max of 5. The member-aware path below is
    // what actually runs, and it reads the real max off the member's own anatomy.
    protected override int CalculateValue(int sourceScore) => Interpolate(sourceScore, 5);

    protected override int CalculateValue(PartyMember member, int sourceScore)
        => Interpolate(sourceScore, member.GetOrganById(RelatedOrganId!)?.MaxScore ?? 0);

    /// <summary>Linear map of <paramref name="score"/> over 0..<paramref name="maxScore"/> onto the lifetime span.</summary>
    private static int Interpolate(int score, int maxScore)
        => maxScore <= 0
            ? MinLifetimeDays
            : MinLifetimeDays + (int)System.Math.Round(
                (double)(MaxLifetimeDays - MinLifetimeDays) * score / maxScore,
                System.MidpointRounding.AwayFromZero);

    public override string FormatValue(int value) => $"{value} d";
}
