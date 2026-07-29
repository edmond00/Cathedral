using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class WoodenSpoon : Item
{
    public override ItemCategory Category => ItemCategory.Tool;
    public override string ItemId      => "wooden_spoon";
    public override string DisplayName => "Wooden Spoon";
    public override string Description => "A long-handled wooden spoon, darkened with use and faintly scorched";
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 4;
}
