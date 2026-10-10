namespace Cathedral.Game.Narrative;

/// <summary>
/// Routine slots, from the anamnesis: how many routines of <b>each kind</b> the protagonist can hold
/// (see <c>RoutineCategory</c>). Every kind fills and evicts on its own, so learning a dozen walks
/// never pushes out the one merchant you know.
///
/// <para>Sized to the routines menu rather than the other way round: the menu lays each kind's slots
/// out in two columns above the map, and four per anamnesis point fills that space at a human's full
/// score of five (twenty slots, ten rows). A bigger number would be slots nobody can see.</para>
/// </summary>
public class RoutineSlotsStat : DerivedStat
{
    public override string Name => "routine_slots";
    public override string DisplayName => "Routine Slots";
    public override string? RelatedOrganId => "anamnesis";

    /// <summary>Slots per kind = anamnesis organ score × 4.</summary>
    protected override int CalculateValue(int sourceScore) => sourceScore * 4;
    public override string FormatValue(int value) => $"{value} per kind";
    public override int WorstValue => 4;
}
