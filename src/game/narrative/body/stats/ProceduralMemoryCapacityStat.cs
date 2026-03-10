namespace Cathedral.Game.Narrative;

/// <summary>
/// Procedural Memory capacity stat. Based on the cerebellum organ score.
/// Determines the number of Procedural Memory slots.
/// </summary>
public class ProceduralMemoryCapacityStat : DerivedStat
{
    public override string Name => "procedural_memory_capacity";
    public override string DisplayName => "Procedural Capacity";
    public override string? RelatedOrganId => "cerebellum";

    /// <summary>Slot count = organ score × 2 (range 2-20).</summary>
    public override int CalculateValue(int sourceScore) => sourceScore * 2;
    public override string FormatValue(int value) => $"{value} slots";
}
