using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Straw : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "straw";
    public override string DisplayName => "Straw";
    public override string Article     => "some";
    public override string Description => "A handful of dry golden straw stalks, hollow and brittle";
    public override List<ItemTag> Tags => new() { ItemTag.Crop };
    public override int PriceReference => 2;
}
