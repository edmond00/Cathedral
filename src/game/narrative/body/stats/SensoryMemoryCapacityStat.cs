namespace Cathedral.Game.Narrative;

/// <summary>
/// Sensory Memory capacity stat. Based on the hippocampus organ score.
/// Determines the number of Sensory Memory slots.
/// </summary>
public class SensoryMemoryCapacityStat : DerivedStat
{
    public override string Name => "sensory_memory_capacity";
    public override string DisplayName => "Sensory Memory";
    public override string? RelatedOrganId => "hippocampus";

    /// <summary>Slot count = organ score × 2 (range 2-20).</summary>
    protected override int CalculateValue(int sourceScore) => sourceScore * 4;
    public override string FormatValue(int value) => $"{value} slots";
    public override int WorstValue => 1;
    public override int? BestValue => 20;
}
