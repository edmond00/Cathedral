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

    /// <summary>
    /// The factor a biome's per-cell encounter chance is multiplied by for this traveller:
    /// 1 − avoidance, so 1.0 with no usable nose (or no traveller) down to 0.7 at nose 3.
    /// The one definition, read by the route forecast (<c>TravelPlanner</c>) and by the
    /// per-cell roll itself — the forecast used to apply it and the roll did not, so a
    /// keen nose lowered the odds the player was shown and not the odds they met.
    /// </summary>
    public static float ChanceFactorFor(PartyMember? traveller)
    {
        if (traveller == null) return 1f;
        var stat = new EncounterAvoidanceStat();
        if (!stat.IsUsable(traveller)) return 1f;
        float avoidance = stat.GetValue(traveller) / 100f;
        return avoidance <= 0f ? 1f : (avoidance >= 1f ? 0f : 1f - avoidance);
    }
}
