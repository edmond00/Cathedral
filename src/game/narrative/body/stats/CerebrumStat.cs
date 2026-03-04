namespace Cathedral.Game.Narrative;

/// <summary>
/// Cerebrum stat. Based on the cerebrum organ score.
/// Determines the number of Semantic Memory slots.
/// </summary>
public class CerebrumStat : DerivedStat
{
    public override string Name => "cerebrum_capacity";
    public override string DisplayName => "Cerebrum";
    public override string? RelatedOrganId => "cerebrum";

    /// <summary>Slot count = organ score × 2 (range 2-20).</summary>
    public override int CalculateValue(int sourceScore) => sourceScore * 2;
}
