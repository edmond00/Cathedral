namespace Cathedral.Game.Narrative;

/// <summary>
/// Number of XP a Modus Mentis needs to gain one level. Derived from the pineal gland organ score.
/// A higher pineal score makes leveling quicker: score 1/2/3 → 12/9/6 XP per level.
/// Lower is better; the value is bounded to 6–12 via <see cref="WorstValue"/> (12, the full
/// cost when the source is weak/absent) and <see cref="BestValue"/> (6, the fastest possible).
/// </summary>
public class ModusMentisXpThresholdStat : DerivedStat
{
    public override string Name => "modus_mentis_xp_threshold";
    public override string DisplayName => "Experience per Level";
    public override string? RelatedOrganId => "pineal_gland";

    public override bool HigherIsBetter => false;
    protected override int CalculateValue(int sourceScore) => 12 - (sourceScore - 1) * 3;
    public override int WorstValue => 12;
    public override int? BestValue => 6;
    public override string FormatValue(int value) => $"{value} XP";
}
