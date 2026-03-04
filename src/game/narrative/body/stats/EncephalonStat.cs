using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Encephalon capacity stat. Based on the total encephalon body part score.
/// Determines the number of Working Memory slots.
/// Replaces the old IntellectStat.
/// </summary>
public class EncephalonStat : DerivedStat
{
    public override string Name => "encephalon_capacity";
    public override string DisplayName => "Encephalon";
    public override string? RelatedBodyPartId => "encephalon";

    /// <summary>
    /// Divide the aggregate encephalon score (sum of 5 organ scores, range 5-50) by 5
    /// to produce a Working Memory capacity of 1-10.
    /// </summary>
    public override int CalculateValue(int sourceScore) => Math.Max(1, sourceScore / 5);
}
