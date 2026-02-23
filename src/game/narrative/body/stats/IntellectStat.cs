namespace Cathedral.Game.Narrative;

/// <summary>
/// Intellect derived stat. Based on brain body part score.
/// Placeholder — replace formula later.
/// </summary>
public class IntellectStat : DerivedStat
{
    public override string Name => "intellect";
    public override string DisplayName => "Intellect";
    public override string? RelatedBodyPartId => "brain";
    
    public override int CalculateValue(int sourceScore) => sourceScore / 2;
}
