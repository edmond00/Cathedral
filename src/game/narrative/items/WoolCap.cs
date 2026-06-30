using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class WoolCap : Item
{
    public override string ItemId           => "wool_cap";
    public override string DisplayName      => "Wool Cap";
    public override string Description      => "A plain knitted wool cap, lumpen and matted with wear";
    public override List<ItemType> Types    => new() { ItemType.Headgear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Headgear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override CoinType PriceCoin => CoinType.Silver;
    public override int PriceReference => 1;
}
