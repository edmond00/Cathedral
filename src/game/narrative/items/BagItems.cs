using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.Items;

// Bags and bundles: a carrying place for every anchor the body has, and a few to stand on the ground.
// Tagged for the traders who would stock them (tanners, drapers, carpenters, farmers), so that they turn up.

public sealed class BeltPouch : WearableContainerItem
{
    public override string ItemId           => "belt_pouch";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName      => "Belt Pouch";
    public override string Description      => "A stiff leather pouch on a belt loop, its flap held by a horn toggle";
    public override ItemSize Size           => ItemSize.Small;
    public override WeightClass Weight      => WeightClass.Insignificant;
    public override WearSlot Slot           => WearSlot.BeltGear;
    public override ContainerKind Kind      => ContainerKind.Storage;
    public override int ContentSlots        => 3;
    public override List<ItemTag> Tags      => new() { ItemTag.Craftware, ItemTag.Pelt, ItemTag.Clothing };
    public override IReadOnlyList<Cathedral.Game.Npc.SocialCategory> DialogueAppeal => new[] { Cathedral.Game.Npc.SocialCategory.Peasant, Cathedral.Game.Npc.SocialCategory.Bourgeois };
}

public sealed class Scrip : WearableContainerItem
{
    public override string ItemId           => "scrip";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName      => "Pilgrim's Scrip";
    public override string Description      => "A soft leather scrip on a long strap, a scallop shell sewn to its flap";
    public override ItemSize Size           => ItemSize.Small;
    public override WeightClass Weight      => WeightClass.Light;
    public override WearSlot Slot           => WearSlot.BeltGear;
    public override ContainerKind Kind      => ContainerKind.Storage;
    public override int ContentSlots        => 4;
    public override List<ItemTag> Tags      => new() { ItemTag.Craftware, ItemTag.Pelt, ItemTag.Devotional };
    public override IReadOnlyList<Cathedral.Game.Npc.SocialCategory> DialogueAppeal => new[] { Cathedral.Game.Npc.SocialCategory.Religious, Cathedral.Game.Npc.SocialCategory.Pauper };
}

public sealed class HerbPouch : WearableContainerItem
{
    public override string ItemId           => "herb_pouch";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName      => "Herb Pouch";
    public override string Description      => "A little linen pouch on a drawstring, green-stained inside and smelling of mint";
    public override ItemSize Size           => ItemSize.Small;
    public override WeightClass Weight      => WeightClass.Insignificant;
    public override WearSlot Slot           => WearSlot.BeltGear;
    public override ContainerKind Kind      => ContainerKind.Storage;
    public override int ContentSlots        => 2;
    public override List<ItemTag> Tags      => new() { ItemTag.Herb, ItemTag.Cloth, ItemTag.Surgery };
    public override IReadOnlyList<Cathedral.Game.Npc.SocialCategory> DialogueAppeal => new[] { Cathedral.Game.Npc.SocialCategory.Peasant, Cathedral.Game.Npc.SocialCategory.Religious };
}

public sealed class Waterskin : WearableContainerItem
{
    public override string ItemId           => "waterskin";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName      => "Waterskin";
    public override string Description      => "A goatskin waterskin, the seams pitched, a wooden stopper on a thong";
    public override ItemSize Size           => ItemSize.Small;
    public override WeightClass Weight      => WeightClass.Light;
    public override WearSlot Slot           => WearSlot.BeltGear;
    public override ContainerKind Kind      => ContainerKind.Vessel;
    public override int ContentSlots        => 3;
    public override List<ItemTag> Tags      => new() { ItemTag.Pelt, ItemTag.Craftware, ItemTag.Foodstuff };
    public override IReadOnlyList<Cathedral.Game.Npc.SocialCategory> DialogueAppeal => new[] { Cathedral.Game.Npc.SocialCategory.Peasant, Cathedral.Game.Npc.SocialCategory.Outlaw };
}

public sealed class NeckPurse : WearableContainerItem
{
    public override string ItemId           => "neck_purse";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName      => "Neck Purse";
    public override string Description      => "A flat leather purse on a cord, worn under the shirt where no cutpurse reaches";
    public override ItemSize Size           => ItemSize.Small;
    public override WeightClass Weight      => WeightClass.Insignificant;
    public override WearSlot Slot           => WearSlot.Neckwear;
    public override ContainerKind Kind      => ContainerKind.Storage;
    public override int ContentSlots        => 2;
    public override List<ItemTag> Tags      => new() { ItemTag.Pelt, ItemTag.Clothing };
    public override IReadOnlyList<Cathedral.Game.Npc.SocialCategory> DialogueAppeal => new[] { Cathedral.Game.Npc.SocialCategory.Bourgeois, Cathedral.Game.Npc.SocialCategory.Pauper };
}

public sealed class BaldricPouch : WearableContainerItem
{
    public override string ItemId           => "baldric_pouch";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName      => "Baldric Pouch";
    public override string Description      => "A pouch riveted to a leather baldric slung across the chest";
    public override ItemSize Size           => ItemSize.Small;
    public override WeightClass Weight      => WeightClass.Light;
    public override WearSlot Slot           => WearSlot.Bodywear;
    public override ContainerKind Kind      => ContainerKind.Storage;
    public override int ContentSlots        => 3;
    public override List<ItemTag> Tags      => new() { ItemTag.Arms, ItemTag.Pelt };
    public override IReadOnlyList<Cathedral.Game.Npc.SocialCategory> DialogueAppeal => new[] { Cathedral.Game.Npc.SocialCategory.Military, Cathedral.Game.Npc.SocialCategory.Outlaw };
}

public sealed class GarterPurse : WearableContainerItem
{
    public override string ItemId           => "garter_purse";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName      => "Garter Purse";
    public override string Description      => "A tiny cloth purse sewn to a garter, for the coin nobody should know about";
    public override ItemSize Size           => ItemSize.Small;
    public override WeightClass Weight      => WeightClass.Insignificant;
    public override WearSlot Slot           => WearSlot.Legwear;
    public override ContainerKind Kind      => ContainerKind.Storage;
    public override int ContentSlots        => 1;
    public override List<ItemTag> Tags      => new() { ItemTag.Clothing, ItemTag.Cloth };
    public override IReadOnlyList<Cathedral.Game.Npc.SocialCategory> DialogueAppeal => new[] { Cathedral.Game.Npc.SocialCategory.Bourgeois, Cathedral.Game.Npc.SocialCategory.Outlaw };
}

public sealed class Knapsack : WearableContainerItem
{
    public override string ItemId           => "knapsack";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName      => "Knapsack";
    public override string Description      => "A canvas knapsack with two shoulder straps, patched at the corners";
    public override ItemSize Size           => ItemSize.Medium;
    public override WeightClass Weight      => WeightClass.Light;
    public override WearSlot Slot           => WearSlot.Outerwear;
    public override ContainerKind Kind      => ContainerKind.Storage;
    public override int ContentSlots        => 6;
    public override List<ItemTag> Tags      => new() { ItemTag.Cloth, ItemTag.Craftware, ItemTag.Textile };
    public override IReadOnlyList<Cathedral.Game.Npc.SocialCategory> DialogueAppeal => new[] { Cathedral.Game.Npc.SocialCategory.Peasant, Cathedral.Game.Npc.SocialCategory.Military };
}

public sealed class BackBasket : WearableContainerItem
{
    public override string ItemId           => "back_basket";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName      => "Back Basket";
    public override string Description      => "A tall wicker basket worn on the back by two rope straps, for the harvest or the market";
    public override ItemSize Size           => ItemSize.Large;
    public override WeightClass Weight      => WeightClass.Light;
    public override WearSlot Slot           => WearSlot.Outerwear;
    public override ContainerKind Kind      => ContainerKind.Storage;
    public override int ContentSlots        => 9;
    public override List<ItemTag> Tags      => new() { ItemTag.Crop, ItemTag.Woodwork };
    public override IReadOnlyList<Cathedral.Game.Npc.SocialCategory> DialogueAppeal => new[] { Cathedral.Game.Npc.SocialCategory.Peasant };
}

public sealed class FramePack : WearableContainerItem
{
    public override string ItemId           => "frame_pack";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName      => "Frame Pack";
    public override string Description      => "A wooden carrying frame lashed with leather, built to bear a load up a mountain path";
    public override ItemSize Size           => ItemSize.Large;
    public override WeightClass Weight      => WeightClass.Medium;
    public override WearSlot Slot           => WearSlot.Outerwear;
    public override ContainerKind Kind      => ContainerKind.Storage;
    public override int ContentSlots        => 9;
    public override List<ItemTag> Tags      => new() { ItemTag.Woodwork, ItemTag.Craftware };
    public override IReadOnlyList<Cathedral.Game.Npc.SocialCategory> DialogueAppeal => new[] { Cathedral.Game.Npc.SocialCategory.Peasant, Cathedral.Game.Npc.SocialCategory.Pauper };
}

public sealed class Satchel : WearableContainerItem
{
    public override string ItemId           => "satchel";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName      => "Satchel";
    public override string Description      => "A canvas satchel on a shoulder strap, ink-stained where the papers rub";
    public override ItemSize Size           => ItemSize.Medium;
    public override WeightClass Weight      => WeightClass.Light;
    public override WearSlot Slot           => WearSlot.BeltGear;
    public override ContainerKind Kind      => ContainerKind.Storage;
    public override int ContentSlots        => 6;
    public override List<ItemTag> Tags      => new() { ItemTag.Cloth, ItemTag.Writing, ItemTag.Clothing };
    public override IReadOnlyList<Cathedral.Game.Npc.SocialCategory> DialogueAppeal => new[] { Cathedral.Game.Npc.SocialCategory.Bourgeois, Cathedral.Game.Npc.SocialCategory.Religious };
}

public sealed class Bundle : WearableContainerItem
{
    public override string ItemId           => "bundle";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName      => "Bundle";
    public override string Description      => "A cloth bundle knotted at the corners and tied to a short stick";
    public override ItemSize Size           => ItemSize.Medium;
    public override WeightClass Weight      => WeightClass.Light;
    public override WearSlot Slot           => WearSlot.Outerwear;
    public override ContainerKind Kind      => ContainerKind.Storage;
    public override int ContentSlots        => 6;
    public override List<ItemTag> Tags      => new() { ItemTag.Cloth, ItemTag.Textile };
    public override IReadOnlyList<Cathedral.Game.Npc.SocialCategory> DialogueAppeal => new[] { Cathedral.Game.Npc.SocialCategory.Pauper, Cathedral.Game.Npc.SocialCategory.Peasant };
}

public sealed class GameBag : WearableContainerItem
{
    public override string ItemId           => "game_bag";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName      => "Game Bag";
    public override string Description      => "A leather game bag with a net front, still feathered from the last hare";
    public override ItemSize Size           => ItemSize.Medium;
    public override WeightClass Weight      => WeightClass.Light;
    public override WearSlot Slot           => WearSlot.BeltGear;
    public override ContainerKind Kind      => ContainerKind.Storage;
    public override int ContentSlots        => 6;
    public override List<ItemTag> Tags      => new() { ItemTag.Pelt, ItemTag.Arms };
    public override IReadOnlyList<Cathedral.Game.Npc.SocialCategory> DialogueAppeal => new[] { Cathedral.Game.Npc.SocialCategory.Peasant, Cathedral.Game.Npc.SocialCategory.Outlaw };
}

public sealed class Saddlebags : ContainerItem
{
    public override string ItemId      => "saddlebags";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName => "Saddlebags";
    public override string Description => "A pair of leather saddlebags joined by a broad strap, buckled shut";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass Weight => WeightClass.Medium;
    public override ContainerKind Kind => ContainerKind.Storage;
    public override int ContentSlots   => 9;
    public override List<ItemTag> Tags => new() { ItemTag.Pelt, ItemTag.Craftware };
}

public sealed class Casket : ContainerItem
{
    public override string ItemId      => "casket";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName => "Casket";
    public override string Description => "A small wooden casket with a hasp, lined with faded cloth";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override ContainerKind Kind => ContainerKind.Storage;
    public override int ContentSlots   => 6;
    public override List<ItemTag> Tags => new() { ItemTag.Woodwork, ItemTag.Craftware };
}

public sealed class NetBag : ContainerItem
{
    public override string ItemId      => "net_bag";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName => "Net Bag";
    public override string Description => "A string bag of knotted cord, light enough to roll into a fist";
    public override ItemSize Size      => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override ContainerKind Kind => ContainerKind.Storage;
    public override int ContentSlots   => 4;
    public override List<ItemTag> Tags => new() { ItemTag.FishingGear, ItemTag.Crop };
}

public sealed class Hamper : ContainerItem
{
    public override string ItemId      => "hamper";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName => "Hamper";
    public override string Description => "A lidded wicker hamper with two handles, for bread and cheese on the road";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass Weight => WeightClass.Medium;
    public override ContainerKind Kind => ContainerKind.Storage;
    public override int ContentSlots   => 9;
    public override List<ItemTag> Tags => new() { ItemTag.Crop, ItemTag.Foodstuff };
}
