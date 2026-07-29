using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Flint : Item
{
    public override ItemCategory Category => ItemCategory.Tool;
    public override string ItemId      => "flint";
    public override string DisplayName => "Flint";
    public override string Description => "A sharp-edged flint nodule, one face knapped flat for striking fire";
    public override int    UsageLevel  => 2;
    public override List<ItemTag> Tags => new() { ItemTag.Mineral };
    public override int PriceReference => 4;
}
