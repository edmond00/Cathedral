using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Working Memory capacity stat. Based on the total encephalon body part score.
/// Determines the number of Working Memory slots.
/// </summary>
public class WorkingMemoryCapacityStat : DerivedStat
{
    public override string Name => "working_memory_capacity";
    public override string DisplayName => "Working Capacity";
    public override string? RelatedBodyPartId => "encephalon";

    /// <summary>
    /// Average the aggregate encephalon score (÷5) then double it
    /// to produce a Working Memory capacity of 2-20.
    /// </summary>
    public override int CalculateValue(int sourceScore) => Math.Max(1, sourceScore / 5 * 2);
    public override string FormatValue(int value) => $"{value} slots";
}
