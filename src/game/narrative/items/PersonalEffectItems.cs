using System.Collections.Generic;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative.Items;

// Small personal belongings that say something about a person rather than about their trade. These
// are what PersonalityTrait uses to make an inventory tell a story: the drinking horn on a drunkard,
// the crutch on a lame man, the beads on someone who prays. Grouped in one file because each is tiny
// and they are only ever read as a set.

/// <summary>A purse is a container first — a belt slot that holds a very little.</summary>
public sealed class CoinPurse : WearableContainerItem
{
    public override string ItemId           => "coin_purse";
    public override string DisplayName      => "Coin Purse";
    public override string Description      => "A small drawstring purse of oiled leather, worn shiny at the mouth";
    public override ItemSize Size           => ItemSize.Small;
    public override WeightClass Weight            => WeightClass.Insignificant;
    public override WearSlot Slot           => WearSlot.BeltGear;
    public override ContainerKind Kind      => ContainerKind.Storage;
    public override int ContentSlots        => 3;
    public override List<ItemTag> Tags      => new() { ItemTag.Craftware };
    public override CoinType PriceCoin      => CoinType.Copper;
    public override int PriceReference      => 5;

    // A purse on the belt says you expect to have coin in it — which reads as substance to a
    // tradesman, and as a mark worth watching to anyone who lives by taking things.
    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Bourgeois, SocialCategory.Outlaw };
}

/// <summary>Stopped with a plug, so it carries as well as it pours — a vessel, not an ornament.</summary>
public sealed class DrinkingHorn : ContainerItem
{
    public override string ItemId      => "drinking_horn";
    public override string DisplayName => "Drinking Horn";
    public override string Description => "A cow's horn stopped with a wooden plug, the rim darkened by many mouths";
    public override ItemSize Size      => ItemSize.Small;
    public override WeightClass Weight       => WeightClass.Light;
    public override ContainerKind Kind => ContainerKind.Vessel;
    public override int ContentSlots   => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 4;
}

public sealed class EyePatch : WearableItem
{
    public override string ItemId           => "eye_patch";
    public override string DisplayName      => "Eye Patch";
    public override string Description      => "A stiff leather patch on a greasy cord, moulded to the shape of a socket";
    public override ItemSize Size           => ItemSize.Small;
    public override WeightClass Weight            => WeightClass.Insignificant;
    public override WearSlot Slot           => WearSlot.Eyewear;
    public override List<ItemTag> Tags      => new() { ItemTag.Clothing };
    public override CoinType PriceCoin      => CoinType.Copper;
    public override int PriceReference      => 2;

    // Reads as a man who has been in something and come out of it — which impresses exactly
    // the two sorts of people who measure each other that way.
    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Outlaw, SocialCategory.Military };
}

public sealed class WoodenCrutch : Item
{
    public override string ItemId      => "wooden_crutch";
    public override string DisplayName => "Wooden Crutch";
    public override string Description => "A forked branch padded at the top with rag and bound with cord";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass Weight       => WeightClass.Medium;
    public override ItemCategory Category => ItemCategory.Tool;
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
    public override WeightClass Weight       => WeightClass.Medium;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Wood };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 4;
}

public sealed class PrayerBeads : WearableItem
{
    public override string ItemId           => "prayer_beads";
    public override string DisplayName      => "String of Beads";
    public override string Description      => "Thirty wooden beads on a knotted cord, three of them rubbed almost featureless";
    public override ItemSize Size           => ItemSize.Small;
    public override WeightClass Weight            => WeightClass.Insignificant;
    public override WearSlot Slot           => WearSlot.Neckwear;
    public override List<ItemTag> Tags      => new() { ItemTag.Craftware };
    public override CoinType PriceCoin      => CoinType.Copper;
    public override int PriceReference      => 3;

    // Three beads worn featureless: whoever carries this has prayed the same three prayers for years.
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Religious };
}

public sealed class LuckCharm : WearableItem
{
    public override string ItemId           => "luck_charm";
    public override string DisplayName      => "Luck Charm";
    public override string Description      => "A hare's foot and a holed pebble tied together on a leather thong";
    public override ItemSize Size           => ItemSize.Small;
    public override WeightClass Weight            => WeightClass.Insignificant;
    public override WearSlot Slot           => WearSlot.Neckwear;
    public override List<ItemTag> Tags      => new() { ItemTag.Craftware };
    public override CoinType PriceCoin      => CoinType.Copper;
    public override int PriceReference      => 2;

    // Country superstition — comforting to those who share it, faintly heretical to the devout.
    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Peasant, SocialCategory.Outlaw };
}

public sealed class BoneDice : Item
{
    public override string ItemId      => "bone_dice";
    public override string DisplayName => "Pair of Bone Dice";
    public override string Description => "Two yellowed dice cut from a knucklebone, the pips picked out in soot";
    public override ItemSize Size      => ItemSize.Small;
    public override WeightClass Weight       => WeightClass.Insignificant;
    public override ItemCategory Category => ItemCategory.Tool;
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
    public override WeightClass Weight       => WeightClass.Insignificant;
    public override ItemCategory Category => ItemCategory.Tool;
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
    public override WeightClass Weight       => WeightClass.Light;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Textile };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 5;
}

/// <summary>
/// Salt is a preserving material rather than a meal — you salt meat with it, you do not sit down
/// to it. Hence <see cref="ItemCategory.Crafting"/>, while its Foodstuff tag keeps it on the
/// victualler's shelf: category and trade tag answer different questions.
/// </summary>
public sealed class SaltPouch : Item
{
    public override string ItemId      => "salt_pouch";
    public override string DisplayName => "Pouch of Salt";
    public override string Description => "A tight little leather pouch of grey salt, hoarded like coin";
    public override ItemSize Size      => ItemSize.Small;
    public override WeightClass Weight       => WeightClass.Light;
    public override ItemCategory Category => ItemCategory.Crafting;
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 6;
}
