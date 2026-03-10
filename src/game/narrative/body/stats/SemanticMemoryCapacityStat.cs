namespace Cathedral.Game.Narrative;

/// <summary>
/// Semantic Memory capacity stat. Based on the cerebrum organ score.
/// Determines the number of Semantic Memory slots.
/// </summary>
public class SemanticMemoryCapacityStat : DerivedStat
{
    public override string Name => "semantic_memory_capacity";
    public override string DisplayName => "Semantic Capacity";
    public override string? RelatedOrganId => "cerebrum";

    /// <summary>Slot count = organ score × 2 (range 2-20).</summary>
    public override int CalculateValue(int sourceScore) => sourceScore * 2;
    public override string FormatValue(int value) => $"{value} slots";
}
