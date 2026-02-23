namespace Cathedral.Game.Narrative;

/// <summary>
/// Willpower derived stat. Based on pineal gland organ score.
/// Placeholder — replace formula later.
/// </summary>
public class WillpowerStat : DerivedStat
{
    public override string Name => "willpower";
    public override string DisplayName => "Willpower";
    public override string? RelatedOrganId => "pineal_gland";
    
    public override int CalculateValue(int sourceScore) => sourceScore;
}
