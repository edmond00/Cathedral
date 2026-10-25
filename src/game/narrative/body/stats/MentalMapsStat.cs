namespace Cathedral.Game.Narrative;

/// <summary>
/// Mental maps, from the anamnesis: how many locations the protagonist remembers at once — each
/// one's map and the routines learned there (see <c>MentalMap</c>). Four per anamnesis point, from
/// none at all to twenty at a human's full score of five; a visit to a place not remembered forgets
/// the oldest unlocked map.
/// </summary>
public class MentalMapsStat : DerivedStat
{
    public override string Name => "mental_maps";
    public override string DisplayName => "Mental Maps";
    public override string? RelatedOrganId => "anamnesis";

    /// <summary>Maps held = anamnesis organ score × 4.</summary>
    protected override int CalculateValue(int sourceScore) => sourceScore * 4;
    public override string FormatValue(int value) => $"{value}";
    public override int WorstValue => 0;
}
