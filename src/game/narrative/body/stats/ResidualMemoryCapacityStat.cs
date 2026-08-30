namespace Cathedral.Game.Narrative;

/// <summary>
/// Residual Memory capacity stat. Based on the anamnesis organ score.
/// Determines the number of Residual Memory slots (the FIFO forgetting queue).
/// </summary>
public class ResidualMemoryCapacityStat : DerivedStat
{
    public override string Name => "residual_memory_capacity";
    public override string DisplayName => "Residual Memory";
    public override string? RelatedOrganId => "anamnesis";

    /// <summary>Slot count = organ score × 2 (range 2-20).</summary>
    protected override int CalculateValue(int sourceScore) => sourceScore * 4;
    public override string FormatValue(int value) => $"{value} slots";
    public override int WorstValue => 1;
    public override int? BestValue => 20;
}
