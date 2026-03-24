namespace Cathedral.Game.Narrative;

/// <summary>
/// Visage — social first impression score derived from the visage body part.
/// Determines the protagonist's initial affinity when entering a dialogue.
/// Source: visage body part aggregate score.
/// Formula: score * 10 (range 0–100, represents a percentage affinity start).
/// </summary>
public class VisageStat : DerivedStat
{
    public override string Name         => "visage";
    public override string DisplayName  => "Visage";
    public override string ShortDisplayName => "VIS";
    public override string? RelatedBodyPartId => "visage";
    public override int CalculateValue(int sourceScore) => sourceScore * 10;
    public override int MinimumValue() => 0;
    public override string FormatValue(int value) => $"{value}";
}
