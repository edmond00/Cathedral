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
    /// Average the aggregate encephalon score (÷5) then double it
    /// to produce a Working Memory capacity of 2-20.
    /// </summary>
    public override int CalculateValue(int sourceScore) => Math.Max(1, sourceScore / 5 * 2);
}
