namespace Cathedral.Game.Narrative;

/// <summary>
/// Teeths organ — ability to chew hard food items.
/// Score 0 (or organ disabled) → cannot eat hard items.
/// Score ≥ 1 → can eat hard items.
///
/// Source: Teeths organ part score (0–2).
/// Formula: score > 0 ? 1 : 0.
/// </summary>
public class TeethHardFoodStat : DerivedStat
{
    public override string Name        => "teeth_hard_food";
    public override string DisplayName => "Chewing Strength";
    public override string? RelatedOrganPartId => "teeths";

    protected override int CalculateValue(int sourceScore) => sourceScore > 0 ? 1 : 0;
    public override string FormatValue(int value) => value > 0 ? "strong" : "weak";
}
