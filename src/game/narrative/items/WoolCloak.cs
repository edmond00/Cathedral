using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class WoolCloak : Item
{
    public override string ItemId           => "wool_cloak";
    public override string DisplayName      => "Wool Cloak";
    public override string Description      => "A heavy earth-brown wool cloak, weather-stained and well-worn";
    public override ItemSize Size           => ItemSize.Large;
    public override List<ItemType> Types    => new() { ItemType.Outerwear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Outerwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override CoinType PriceCoin => CoinType.Silver;
    public override int PriceReference => 2;
}
