namespace Cathedral.Game.Narrative;

/// <summary>
/// Visage — social first impression score derived from the visage body part.
/// Determines the protagonist's initial affinity when entering a dialogue.
/// Source: visage body part aggregate score.
/// Formula: score * 2 (range 0–100, represents a percentage affinity start).
/// </summary>
public class BeautyStat : DerivedStat
{
    public override string Name         => "beauty";
    public override string DisplayName  => "Beauty";
    public override string ShortDisplayName => "Beauty";
    public override string? RelatedBodyPartId => "visage";
    public override int CalculateValue(int sourceScore) => sourceScore * 2;
    public override int MinimumValue() => 0;
    public override string FormatValue(int value) => $"{value}";
}
