using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Hatchet : Item
{
    public override ItemCategory Category => ItemCategory.Tool;
    public override string ItemId      => "hatchet";
    public override string DisplayName => "Hatchet";
    public override string Description => "A small single-bit hatchet, the haft smooth from long use";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass Weight       => WeightClass.Medium;
    public override int   UsageLevel   => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 10;
}
