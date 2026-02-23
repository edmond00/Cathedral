namespace Cathedral.Game.Narrative;

/// <summary>
/// Dexterity derived stat. Based on hands organ score.
/// Placeholder — replace formula later.
/// </summary>
public class DexterityStat : DerivedStat
{
    public override string Name => "dexterity";
    public override string DisplayName => "Dexterity";
    public override string? RelatedOrganId => "hands";
    
    public override int CalculateValue(int sourceScore) => sourceScore;
}
