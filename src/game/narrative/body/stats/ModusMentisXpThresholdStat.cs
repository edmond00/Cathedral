using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Number of XP a Modus Mentis needs to gain one level. Derived from the pineal gland organ score.
/// A higher pineal score makes leveling quicker: score 1/2/3 → 12/9/6 XP per level
/// (clamped to 6-12 so an absent/0 source still costs the full 12).
/// </summary>
public class ModusMentisXpThresholdStat : DerivedStat
{
    public override string Name => "modus_mentis_xp_threshold";
    public override string DisplayName => "MM XP per Level";
    public override string? RelatedOrganId => "pineal_gland";

    public override int CalculateValue(int sourceScore) => Math.Clamp(12 - (sourceScore - 1) * 3, 6, 12);
    public override int MinimumValue() => 12;
    public override string FormatValue(int value) => $"{value} XP";
}
