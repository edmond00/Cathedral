namespace Cathedral.Game.Narrative;

/// <summary>
/// Sensory Memory capacity stat. Based on the hippocampus organ score.
/// Determines the number of Sensory Memory slots.
/// </summary>
public class SensoryMemoryCapacityStat : DerivedStat
{
    public override string Name => "sensory_memory_capacity";
    public override string DisplayName => "Sensory Capacity";
    public override string? RelatedOrganId => "hippocampus";

    /// <summary>Slot count = organ score × 2 (range 2-20).</summary>
    public override int CalculateValue(int sourceScore) => sourceScore * 2;
    public override string FormatValue(int value) => $"{value} slots";
}
