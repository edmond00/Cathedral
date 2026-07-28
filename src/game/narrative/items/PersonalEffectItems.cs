using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

// Small personal belongings that say something about a person rather than about their trade. These
// are what PersonalityTrait uses to make an inventory tell a story: the drinking horn on a drunkard,
// the crutch on a lame man, the beads on someone who prays. Grouped in one file because each is tiny
// and they are only ever read as a set.

public sealed class CoinPurse : Item
{
    public override string ItemId           => "coin_purse";
    public override string DisplayName      => "Coin Purse";
    public override string Description      => "A small drawstring purse of oiled leather, worn shiny at the mouth";
    public override ItemSize Size           => ItemSize.Small;
    public override float Weight            => 0.1f;
    public override List<ItemType> Types    => new() { ItemType.BeltGear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.BeltGear;
    public override List<ItemTag> Tags      => new() { ItemTag.Craftware };
    public override CoinType PriceCoin      => CoinType.Copper;
    public override int PriceReference      => 5;
}

public sealed class DrinkingHorn : Item
{
    public override string ItemId      => "drinking_horn";
    public override string DisplayName => "Drinking Horn";
    public override string Description => "A cow's horn stopped with a wooden plug, the rim darkened by many mouths";
    public override ItemSize Size      => ItemSize.Small;
    public override float Weight       => 0.3f;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 4;
}

public sealed class EyePatch : Item
{
    public override string ItemId           => "eye_patch";
    public override string DisplayName      => "Eye Patch";
    public override string Description      => "A stiff leather patch on a greasy cord, moulded to the shape of a socket";
    public override ItemSize Size           => ItemSize.Small;
    public override float Weight            => 0.05f;
    public override List<ItemType> Types    => new() { ItemType.Eyewear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Eyewear;
    public override List<ItemTag> Tags      => new() { ItemTag.Clothing };
    public override CoinType PriceCoin      => CoinType.Copper;
    public override int PriceReference      => 2;
}

public sealed class WoodenCrutch : Item
{
    public override string ItemId      => "wooden_crutch";
    public override string DisplayName => "Wooden Crutch";
    public override string Description => "A forked branch padded at the top with rag and bound with cord";
    public override ItemSize Size      => ItemSize.Large;
    public override float Weight       => 1.0f;
    public override int   UsageLevel   => 2;
    public override List<ItemTag> Tags => new() { ItemTag.Wood };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 3;
}

public sealed class WalkingStaff : Item
{
    public override string ItemId      => "walking_staff";
    public override string DisplayName => "Walking Staff";
    public override string Description => "A shoulder-high staff of seasoned ash, the foot shod with a scrap of iron";
    public override ItemSize Size      => ItemSize.Large;
    public override float Weight       => 1.0f;
    public override int   UsageLevel   => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Wood };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 4;
}

public sealed class PrayerBeads : Item
{
    public override string ItemId           => "prayer_beads";
    public override string DisplayName      => "String of Beads";
    public override string Description      => "Thirty wooden beads on a knotted cord, three of them rubbed almost featureless";
    public override ItemSize Size           => ItemSize.Small;
    public override float Weight            => 0.08f;
    public override List<ItemType> Types    => new() { ItemType.Neckwear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Neckwear;
    public override List<ItemTag> Tags      => new() { ItemTag.Craftware };
    public override CoinType PriceCoin      => CoinType.Copper;
    public override int PriceReference      => 3;
}

public sealed class LuckCharm : Item
{
    public override string ItemId           => "luck_charm";
    public override string DisplayName      => "Luck Charm";
    public override string Description      => "A hare's foot and a holed pebble tied together on a leather thong";
    public override ItemSize Size           => ItemSize.Small;
    public override float Weight            => 0.05f;
    public override List<ItemType> Types    => new() { ItemType.Neckwear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Neckwear;
    public override List<ItemTag> Tags      => new() { ItemTag.Craftware };
    public override CoinType PriceCoin      => CoinType.Copper;
    public override int PriceReference      => 2;
}

public sealed class BoneDice : Item
{
    public override string ItemId      => "bone_dice";
    public override string DisplayName => "Pair of Bone Dice";
    public override string Description => "Two yellowed dice cut from a knucklebone, the pips picked out in soot";
    public override ItemSize Size      => ItemSize.Small;
    public override float Weight       => 0.03f;
    public override int   UsageLevel   => 2;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 2;
}

public sealed class WoodenPipe : Item
{
    public override string ItemId      => "wooden_pipe";
    public override string DisplayName => "Reed Pipe";
    public override string Description => "A five-holed pipe cut from elder, the mouthpiece worn oval";
    public override ItemSize Size      => ItemSize.Small;
    public override float Weight       => 0.06f;
    public override int   UsageLevel   => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 4;
}

public sealed class MendingKit : Item
{
    public override string ItemId      => "mending_kit";
    public override string DisplayName => "Mending Kit";
    public override string Description => "A rag roll holding needles, waxed thread and a fistful of odd patches";
    public override ItemSize Size      => ItemSize.Small;
    public override float Weight       => 0.2f;
    public override int   UsageLevel   => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Textile };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 5;
}

public sealed class SaltPouch : Item
{
    public override string ItemId      => "salt_pouch";
    public override string DisplayName => "Pouch of Salt";
    public override string Description => "A tight little leather pouch of grey salt, hoarded like coin";
    public override ItemSize Size      => ItemSize.Small;
    public override float Weight       => 0.15f;
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 6;
}
