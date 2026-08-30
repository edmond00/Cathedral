namespace Cathedral.Game.Narrative;

/// <summary>
/// Max Companions — how many companions the protagonist's heart can sustain in the party.
/// Source: heart organ (trunk).
/// Formula: 1 companion per heart level (0–5).
///
/// Checked at the start of each world-travel phase: if the party holds more companions
/// than this, the player must dismiss some before choosing a destination
/// (see CompanionRemovalRenderer).
/// </summary>
public class MaxCompanionsStat : DerivedStat
{
    public override string Name         => "max_companions";
    public override string DisplayName  => "Max Companions";
    public override string? RelatedOrganId => "heart";
    protected override int CalculateValue(int sourceScore) => sourceScore;
    public override string FormatValue(int value) => $"{value} companions";
}
