namespace Cathedral.Game.Narrative;

/// <summary>
/// Tool Usage Cap — the maximum <see cref="Item.UsageLevel"/> that contributes bonus dice when a
/// tool is combined with an action during the narration phase.
/// Source: hands organ (upper_limbs).
/// Formula: cap = hands score (0–6). Steady hands extract the full potency of a specialised tool;
/// weak or wounded ones clamp the bonus down however good the tool is.
///
/// Applied when building the SyntheticItemModusMentis in
/// <c>NarrativeController.ExecuteItemCombinationAsync</c>: the effective usage level is
/// <c>min(item.UsageLevel, ToolUsageCap)</c>. A disabled (high-handicap wound) hands organ caps at
/// 0, meaning a combined tool grants no dice bonus at all.
/// </summary>
public class ToolUsageCapStat : DerivedStat
{
    public override string Name         => "tool_usage_cap";
    public override string DisplayName  => "Tool Usage Cap";
    public override string? RelatedOrganId => "hands";

    protected override int CalculateValue(int sourceScore) => sourceScore;
    public override string FormatValue(int value) => $"lv. {value}";
}
