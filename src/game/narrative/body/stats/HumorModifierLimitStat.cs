namespace Cathedral.Game.Narrative;

/// <summary>
/// Humor Modifier Limit — how many humor transmuting virtues the player may apply to a single
/// dice roll before confirming the result.
/// Source: viscera organ (trunk), whose max score is 4 — so 1–4 modifiers per roll.
/// Formula: limit = viscera score (clamped to a minimum of 1 so a roll can always be tweaked
/// at least once even with a weak or wounded viscera).
///
/// Read at roll time by <see cref="Cathedral.Game.DiceRollComponent"/> consumers
/// (narration, dialogue, combat) to cap how many tail humors can be spent.
/// </summary>
public class HumorModifierLimitStat : DerivedStat
{
    public override string Name         => "humor_modifier_limit";
    public override string DisplayName  => "Humor Modifier Limit";
    public override string? RelatedOrganId => "viscera";

    public override int CalculateValue(int sourceScore) => System.Math.Max(1, sourceScore);
    public override int MinimumValue() => 1;
    public override string FormatValue(int value) => $"{value}/roll";
}
