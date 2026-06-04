namespace Cathedral.Game.Narrative;

/// <summary>
/// Max Companions — how many companions the protagonist's heart can sustain in the party.
/// Source: heart organ (trunk).
/// Formula: 2 companions per heart level.
///
/// Checked at the start of each world-travel phase: if the party holds more companions
/// than this, the player must dismiss some before choosing a destination
/// (see CompanionRemovalRenderer).
/// </summary>
public class MaxCompanionsStat : DerivedStat
{
    public override string Name         => "max_companions";
    public override string DisplayName  => "Max Companions";
    public override string ShortDisplayName => "Companions";
    public override string? RelatedOrganId => "heart";
    public override int CalculateValue(int sourceScore) => sourceScore * 2;
    public override string FormatValue(int value) => $"{value}";
}
