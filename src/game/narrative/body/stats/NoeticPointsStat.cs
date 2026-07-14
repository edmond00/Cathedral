namespace Cathedral.Game.Narrative;

/// <summary>
/// Noetic Points — the pool of thinking attempts a member has per narration node (each
/// observation/thinking/action that can fail spends one). Based on the total encephalon body
/// part score: one noetic point per encephalon level.
/// Source: encephalon body part (aggregate score).
/// Formula: score (floored at 1 so a member can always attempt at least one action).
/// </summary>
public class NoeticPointsStat : DerivedStat
{
    public override string Name => "noetic_points";
    public override string DisplayName => "Noetic Points";
    public override string? RelatedBodyPartId => "encephalon";

    protected override int CalculateValue(int sourceScore) => sourceScore;
    public override int WorstValue => 1;
    public override string FormatValue(int value) => $"{value} pts";
}
