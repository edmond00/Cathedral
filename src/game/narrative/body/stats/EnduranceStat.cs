namespace Cathedral.Game.Narrative;

/// <summary>
/// Endurance derived stat. Based on trunk body part score.
/// Placeholder — replace formula later.
/// </summary>
public class EnduranceStat : DerivedStat
{
    public override string Name => "endurance";
    public override string DisplayName => "Endurance";
    public override string? RelatedBodyPartId => "trunk";
    
    public override int CalculateValue(int sourceScore) => sourceScore / 3;
}
