namespace Cathedral.Game.Narrative;

/// <summary>
/// Hippocampus stat. Based on the hippocampus organ score.
/// Determines the number of Sensory Memory slots.
/// </summary>
public class HippocampusStat : DerivedStat
{
    public override string Name => "hippocampus_capacity";
    public override string DisplayName => "Hippocampus";
    public override string? RelatedOrganId => "hippocampus";

    /// <summary>Slot count = raw score directly.</summary>
    public override int CalculateValue(int sourceScore) => sourceScore;
}
