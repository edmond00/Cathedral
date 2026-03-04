namespace Cathedral.Game.Narrative;

/// <summary>
/// Cerebellum stat. Based on the cerebellum organ score.
/// Determines the number of Procedural Memory slots.
/// </summary>
public class CerebellumStat : DerivedStat
{
    public override string Name => "cerebellum_capacity";
    public override string DisplayName => "Cerebellum";
    public override string? RelatedOrganId => "cerebellum";

    /// <summary>Slot count = organ score × 2 (range 2-20).</summary>
    public override int CalculateValue(int sourceScore) => sourceScore * 2;
}
