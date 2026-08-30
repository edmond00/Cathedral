namespace Cathedral.Game.Narrative;

/// <summary>
/// Routine queue size stat. Based on the anamnesis organ score. Determines how many learned
/// routines the protagonist can retain in the FIFO routine queue.
/// </summary>
public class RoutineQueueSizeStat : DerivedStat
{
    public override string Name => "routine_queue_size";
    public override string DisplayName => "Routine Queue";
    public override string? RelatedOrganId => "anamnesis";

    /// <summary>Routine capacity = anamnesis organ score × 10.</summary>
    protected override int CalculateValue(int sourceScore) => sourceScore * 10;
    public override string FormatValue(int value) => $"{value} routines";
    public override int WorstValue => 10;
}
