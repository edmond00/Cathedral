namespace Cathedral.Game.Narrative;

/// <summary>
/// Nose organ — travel encounter avoidance. A keen nose smells danger ahead and
/// steers the party clear, reducing the per-cell encounter chance contributed by
/// the biomes crossed during travel.
///
/// Source: Nose organ score (0–3).
/// Formula: 10% reduction per level — 0→0%, 1→10%, 2→20%, 3→30%.
/// </summary>
public class EncounterAvoidanceStat : DerivedStat
{
    public override string Name        => "encounter_avoidance";
    public override string DisplayName => "Encounter Avoidance";
    public override string? RelatedOrganId => "nose";

    protected override int CalculateValue(int sourceScore) => sourceScore * 10;
    public override string FormatValue(int value) => $"{value}%";
}
