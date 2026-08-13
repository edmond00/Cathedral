namespace Cathedral.Game.Narrative;

/// <summary>
/// Visage — the favour a face earns on a first acquaintance, counted in dialogue dice.
/// Every conversation's check adds these to the pool alongside the NPC's affinity and what
/// the speaker is wearing (see <c>DialogueTreeController.BeginResolution</c>).
/// Source: visage body part aggregate score.
/// Formula: one die per 5 points of visage score — a maxed visage (22) is worth 4 dice.
/// </summary>
public class BeautyStat : DerivedStat
{
    public override string Name         => "beauty";
    public override string DisplayName  => "Countenance";
    public override string? RelatedBodyPartId => "visage";
    protected override int CalculateValue(int sourceScore) => sourceScore / 5;
    public override string FormatValue(int value) => $"{value} dice";
}
