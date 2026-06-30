using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class LeatherGloves : Item
{
    public override string ItemId           => "leather_gloves";
    public override string DisplayName      => "Leather Gloves";
    public override string Description      => "A pair of stiff work gloves in thick undyed leather, cracked at the knuckles";
    public override List<ItemType> Types    => new() { ItemType.Handwear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.RightHandwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override CoinType PriceCoin => CoinType.Silver;
    public override int PriceReference => 1;
}
