using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Sickle : Item
{
    public override ItemCategory Category => ItemCategory.Tool;
    public override string ItemId      => "sickle";
    public override string DisplayName => "Sickle";
    public override string Description => "A short iron sickle with a curved blade, edge nicked from years of harvest";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass Weight       => WeightClass.Light;
    public override int   UsageLevel   => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 10;
}
