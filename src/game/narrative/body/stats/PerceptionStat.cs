namespace Cathedral.Game.Narrative;

/// <summary>
/// Perception derived stat. Based on visage body part score.
/// Placeholder — replace formula later.
/// </summary>
public class PerceptionStat : DerivedStat
{
    public override string Name => "perception";
    public override string DisplayName => "Perception";
    public override string? RelatedBodyPartId => "visage";
    
    public override int CalculateValue(int sourceScore) => sourceScore / 2;
}
