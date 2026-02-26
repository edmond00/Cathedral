namespace Cathedral.Game.Narrative;

/// <summary>
/// Intellect derived stat. Based on encephalon body part score.
/// Placeholder — replace formula later.
/// </summary>
public class IntellectStat : DerivedStat
{
    public override string Name => "intellect";
    public override string DisplayName => "Intellect";
    public override string? RelatedBodyPartId => "encephalon";
    
    public override int CalculateValue(int sourceScore) => sourceScore / 2;
}
