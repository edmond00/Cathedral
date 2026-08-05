namespace Cathedral.Game.Narrative;

/// <summary>
/// Wound healing — how many days the body needs to close a wound it can close.
/// Derived from the viscera, "the constitution's temper and fortitude" (see <see cref="VisceraOrgan"/>).
///
/// <para>
/// This stat replaced <c>damage_resistance</c>, and inverts what it meant. Resistance was a hidden
/// roll made the instant a blow landed, quietly downgrading the wound's severity; the player never
/// saw it and could not plan around it. Constitution now shows itself over the long run instead:
/// a tough body takes the same wound as a frail one, and gets over it sooner.
/// </para>
///
/// <para>
/// <b>Lower is better</b> — the value is a duration, not a score. It runs from
/// <see cref="SlowestHealDays"/> at viscera score 0 down to <see cref="FastestHealDays"/> at the
/// species maximum, scaled linearly in between. Scaling against the member's own viscera max
/// (rather than a hard-coded 5) keeps the range correct for beasts, whose viscera tops out
/// elsewhere.
/// </para>
///
/// <para>
/// <b>Wounds slow healing.</b> Like <see cref="LifetimeStat"/> this goes through the wound-aware
/// path, so a wound to the viscera itself lengthens the recovery of every other wound — and a
/// disabling one drops the body to <see cref="SlowestHealDays"/>. Only Low and Medium wounds ever
/// heal; High wounds are permanent regardless of this stat (see <see cref="PartyMember.HealWounds"/>).
/// </para>
/// </summary>
public sealed class WoundHealingStat : DerivedStat
{
    /// <summary>Slowest recovery: a frail body needs roughly three years to mend.</summary>
    public const int SlowestHealDays = 1000;

    /// <summary>Fastest recovery: a hardened body mends in under a year.</summary>
    public const int FastestHealDays = 100;

    public override string Name             => "wound_healing";
    public override string DisplayName      => "Wound Healing";
    public override string ShortDisplayName => "Wound Healing";
    public override string? RelatedOrganId  => "viscera";

    /// <summary>A duration: fewer days is a healthier body.</summary>
    public override bool HigherIsBetter => false;

    public override int WorstValue => SlowestHealDays;
    public override int? BestValue => FastestHealDays;

    // Member-agnostic fallback: assume the human viscera max of 5. The member-aware path below is
    // what actually runs, and it reads the real max off the member's own anatomy.
    protected override int CalculateValue(int sourceScore) => Interpolate(sourceScore, 5);

    protected override int CalculateValue(PartyMember member, int sourceScore)
        => Interpolate(sourceScore, member.GetOrganById(RelatedOrganId!)?.MaxScore ?? 0);

    /// <summary>Linear map of <paramref name="score"/> over 0..<paramref name="maxScore"/> onto the healing span.</summary>
    private static int Interpolate(int score, int maxScore)
        => maxScore <= 0
            ? SlowestHealDays
            : SlowestHealDays - (int)System.Math.Round(
                (double)(SlowestHealDays - FastestHealDays) * score / maxScore,
                System.MidpointRounding.AwayFromZero);

    public override string FormatValue(int value) => $"{value} d";
}
