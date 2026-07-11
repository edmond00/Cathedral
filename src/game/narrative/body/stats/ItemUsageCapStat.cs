namespace Cathedral.Game.Narrative;

/// <summary>
/// Item Usage Cap — the maximum item <see cref="Item.UsageLevel"/> that contributes bonus
/// dice when an item is combined with an action during the narration phase.
/// Source: hands organ (upper_limbs).
/// Formula: cap = hands score (0–10). A character whose hands are stronger can extract the
/// full potency of high-UsageLevel tools, while weak or wounded hands clamp the bonus down.
///
/// Applied when building the SyntheticItemModusMentis in
/// <c>NarrativeController.ExecuteItemCombinationAsync</c>: the item's effective usage level is
/// <c>min(item.UsageLevel, ItemUsageCap)</c>. A disabled (high-handicap wound) hands organ
/// caps at 0, meaning combined items grant no dice bonus at all.
/// </summary>
public class ItemUsageCapStat : DerivedStat
{
    public override string Name         => "item_usage_cap";
    public override string DisplayName  => "Item Usage Cap";
    public override string? RelatedOrganId => "hands";

    protected override int CalculateValue(int sourceScore) => sourceScore;
    public override string FormatValue(int value) => $"lv. {value}";
}
