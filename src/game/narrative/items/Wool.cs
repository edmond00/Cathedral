using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Wool : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "wool";
    public override string DisplayName => "Wool";
    public override string Article     => "some";
    public override string Description => "A loose fleece of raw sheep's wool, greasy with lanolin";
    public override ItemSize Size      => ItemSize.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Textile };
    public override int PriceReference => 8;
}
