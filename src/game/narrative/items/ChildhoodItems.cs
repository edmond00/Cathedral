using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative.Items;

// ──────────────────────────────────────────────────────────────────────────────
// Items granted by REMEMBER actions during the childhood reminescence phase.
// Clothing types from the design draft are split into multiple pieces (a
// "noble clothing" outcome materialises as silk stockings, a knee-length coat,
// a noble undertunic, etc.) so the protagonist's anchors fill in a coherent way.
// ──────────────────────────────────────────────────────────────────────────────

// Childhood dress is the clearest statement of where someone comes from, so nearly all of it
// earns its keep through DialogueAppeal rather than protection. The exceptions are the wooden
// clogs, which are genuinely armoured by accident.

// ── Stable child clothes ──────────────────────────────────────────────
public sealed class StableChildSmock : WearableItem
{
    public override string ItemId           => "stable_child_smock";
    public override string DisplayName      => "Stable Child Smock";
    public override string Description      => "A coarse linen smock stained with hay-dust and oat husks";
    public override ItemSize Size           => ItemSize.Medium;
    public override WearSlot Slot           => WearSlot.Bodywear;
    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Peasant, SocialCategory.Pauper };
}

public sealed class StableChildBreeches : WearableItem
{
    public override string ItemId           => "stable_child_breeches";
    public override string DisplayName      => "Stable Child Breeches";
    public override string Description      => "Knee-length wool breeches patched at the seat";
    public override WearSlot Slot           => WearSlot.Legwear;
    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Peasant, SocialCategory.Pauper };
}

public sealed class StableChildClogs : WearableItem
{
    public override string ItemId           => "stable_child_clogs";
    public override string DisplayName      => "Wooden Clogs";
    public override string Description      => "Heavy wooden clogs, soled with old straw padding";
    public override WearSlot Slot           => WearSlot.Footwear;
    // Solid blocks of wood around the foot: clumsy, but nothing short of an axe gets through them.
    public override int DefenseDice => 1;
    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Peasant, SocialCategory.Pauper };
}

// ── Townsman clothes ──────────────────────────────────────────────────
public sealed class TownsmanCloak : WearableItem
{
    public override string ItemId           => "townsman_cloak";
    public override string DisplayName      => "Townsman Cloak";
    public override string Description      => "A serviceable hooded cloak of plain dark wool";
    public override ItemSize Size           => ItemSize.Medium;
    public override WearSlot Slot           => WearSlot.Outerwear;
    public override int DefenseDice => 1;
    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Bourgeois, SocialCategory.Urban };
}

public sealed class TownsmanTunic : WearableItem
{
    public override string ItemId           => "townsman_tunic";
    public override string DisplayName      => "Townsman Tunic";
    public override string Description      => "A plain belted tunic of undyed linen";
    public override ItemSize Size           => ItemSize.Medium;
    public override WearSlot Slot           => WearSlot.Bodywear;
    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Bourgeois, SocialCategory.Urban };
}

public sealed class TownsmanBreeches : WearableItem
{
    public override string ItemId           => "townsman_breeches";
    public override string DisplayName      => "Townsman Breeches";
    public override string Description      => "Knee-length grey breeches of close-woven wool";
    public override WearSlot Slot           => WearSlot.Legwear;
    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Bourgeois, SocialCategory.Urban };
}

public sealed class TownsmanCap : WearableItem
{
    public override string ItemId           => "townsman_cap";
    public override string DisplayName      => "Townsman Cap";
    public override string Description      => "A felt cap with a turned-up brim";
    public override WearSlot Slot           => WearSlot.Headgear;
    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Bourgeois, SocialCategory.Urban };
}

// ── Plain robe (orphanage / temple) ───────────────────────────────────
public sealed class PlainRobe : WearableItem
{
    public override string ItemId           => "plain_robe";
    public override string DisplayName      => "Plain Robe";
    public override string Description      => "A long undyed wool robe with a knotted cord at the waist";
    public override ItemSize Size           => ItemSize.Large;
    public override WearSlot Slot           => WearSlot.Outerwear;
    // The knotted cord is the tell: this is temple dress, and those who know it read it instantly.
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Religious };
}

// ── Farmer clothing ───────────────────────────────────────────────────
public sealed class FarmerSmock : WearableItem
{
    public override string ItemId           => "farmer_smock";
    public override string DisplayName      => "Farmer Smock";
    public override string Description      => "A heavy linen smock smelling faintly of grain and barn";
    public override ItemSize Size           => ItemSize.Medium;
    public override WearSlot Slot           => WearSlot.Bodywear;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Peasant };
}

public sealed class FarmerBreeches : WearableItem
{
    public override string ItemId           => "farmer_breeches";
    public override string DisplayName      => "Farmer Breeches";
    public override string Description      => "Sturdy wool breeches, knee-tied with leather thongs";
    public override WearSlot Slot           => WearSlot.Legwear;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Peasant };
}

public sealed class FarmerStrawHat : WearableItem
{
    public override string ItemId           => "farmer_straw_hat";
    public override string DisplayName      => "Straw Hat";
    public override string Description      => "A wide-brimmed straw hat, bleached pale by sun";
    public override WearSlot Slot           => WearSlot.Headgear;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Peasant };
}

public sealed class FarmerClogs : WearableItem
{
    public override string ItemId           => "farmer_clogs";
    public override string DisplayName      => "Farmer Clogs";
    public override string Description      => "Caked wooden clogs, heavy and serviceable";
    public override WearSlot Slot           => WearSlot.Footwear;
    public override int DefenseDice => 1;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Peasant };
}

// ── Noble clothing ────────────────────────────────────────────────────
public sealed class SilkStockings : WearableItem
{
    public override string ItemId           => "silk_stockings";
    public override string DisplayName      => "Silk Stockings";
    public override string Description      => "A pair of pale-grey silk stockings, finely knitted";
    public override WearSlot Slot           => WearSlot.Legwear;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Aristocrat };
}

public sealed class KneeLengthCoat : WearableItem
{
    public override string ItemId           => "knee_length_coat";
    public override string DisplayName      => "Knee-length Coat";
    public override string Description      => "A panelled coat of dark wool trimmed in velvet";
    public override ItemSize Size           => ItemSize.Large;
    public override WearSlot Slot           => WearSlot.Outerwear;
    public override int DefenseDice => 1;
    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Aristocrat, SocialCategory.Bourgeois };
}

public sealed class NobleUndertunic : WearableItem
{
    public override string ItemId           => "noble_undertunic";
    public override string DisplayName      => "Noble Undertunic";
    public override string Description      => "A fine ivory linen undertunic with embroidered cuffs";
    public override ItemSize Size           => ItemSize.Medium;
    public override WearSlot Slot           => WearSlot.Bodywear;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Aristocrat };
}

public sealed class SoftLeatherShoes : WearableItem
{
    public override string ItemId           => "soft_leather_shoes";
    public override string DisplayName      => "Soft Leather Shoes";
    public override string Description      => "Thin-soled shoes of supple dyed leather, not made for rough roads";
    public override WearSlot Slot           => WearSlot.Footwear;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Aristocrat };
}

// ── Travelling supplies (curiosity / dream / gold_thirst) ─────────────
public sealed class TravelersBackpack : WearableContainerItem
{
    public override string ItemId           => "travelers_backpack";
    public override string DisplayName      => "Travellers' Backpack";
    public override string Description      => "A heavy canvas backpack with leather straps, sized for the road";
    public override ItemSize Size           => ItemSize.Large;
    public override WearSlot Slot           => WearSlot.Outerwear;
    public override ContainerKind Kind      => ContainerKind.Storage;
    public override int    ContentSlots     => 18;

    // Everything owned, carried on the back: the mark of someone with no roof to leave it under.
    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Pauper, SocialCategory.Outlaw };
}

public sealed class Sausage : ConsumableItem
{
    public override string ItemId      => "sausage";
    public override string DisplayName => "Sausage";
    public override string Description => "A coil of cured pork sausage, dark and pungent";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Rich;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(40).Add<FatHumor>(35).Add<SaltHumor>(25);
}

public sealed class LeatherCanteen : WearableContainerItem
{
    public override string ItemId           => "leather_canteen";
    public override string DisplayName      => "Leather Canteen";
    public override string Description      => "A wax-treated leather canteen on a long shoulder strap";
    public override ItemSize Size           => ItemSize.Small;
    public override WearSlot Slot           => WearSlot.BeltGear;
    public override ContainerKind Kind      => ContainerKind.Vessel;
    public override int    ContentSlots     => 3;

    // Carrying your own water marks you as someone who walks between places rather than living
    // in one — read sympathetically by those who do the same, and by those with nowhere to be.
    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Peasant, SocialCategory.Pauper };
}

public sealed class WaterDraught : ConsumableItem
{
    public override string ItemId      => "water_draught";
    public override string DisplayName => "Water";
    public override string Description => "Cool fresh water";
    public override ConsumableType ConsumableType => ConsumableType.Drink;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AquaHumor>(100);
}

// ── Sundries ──────────────────────────────────────────────────────────
public sealed class Hairpin : Item
{
    public override string ItemId      => "hairpin";
    public override string DisplayName => "Hairpin";
    public override string Description => "A slim brass hairpin, bent into a useful pick";
    public override ItemCategory Category => ItemCategory.Tool;
    public override int    UsageLevel  => 2;
}

/// <summary>
/// A real blade, so it is a real weapon: as a plain <c>Item</c> it could be carried but never
/// fought with, which is not what a sword is for.
/// </summary>
public sealed class ShortSword : WeaponItem
{
    public override string ItemId           => "short_sword";
    public override string DisplayName      => "Short Sword";
    public override string Description      => "A plain straight-bladed short sword in a worn leather scabbard";
    public override ItemSize Size           => ItemSize.Medium;
    public override int    Level            => 1;
    public override string WeaponCategory   => "short_blade";
}

public sealed class WoodenStick : Item
{
    public override string ItemId      => "wooden_stick";
    public override string DisplayName => "Wooden Stick";
    public override string Description => "A weather-greyed stick still half-imagined as a magic sword";
    public override ItemCategory Category => ItemCategory.Tool;
    public override int    UsageLevel  => 1;
}

/// <summary>
/// Genuinely nothing but a keepsake — no use, no protection, no trade. <see cref="ItemCategory.Other"/>
/// is the honest answer rather than forcing it into a category it does not belong to.
/// </summary>
public sealed class WoodenDoll : Item
{
    public override string ItemId      => "wooden_doll";
    public override string DisplayName => "Wooden Doll";
    public override string Description => "A small, lovingly worn doll of carved oak — a sleeping princess once";
}

public sealed class Worm : ConsumableItem
{
    public override string ItemId      => "worm";
    public override string DisplayName => "Worm";
    public override string Description => "A long pale earthworm, still squirming";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(40).Add<FungiHumor>(35).Add<YellowBileHumor>(25);
}

public sealed class MouseMeat : ConsumableItem
{
    public override string ItemId      => "mouse_meat";
    public override string DisplayName => "Mouse Meat";
    public override string Article => "some";
    public override string Description => "A scrap of stringy mouse flesh, scarcely a mouthful";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(55).Add<FiberHumor>(25).Add<FatHumor>(20);
}

public sealed class SquirrelMeat : ConsumableItem
{
    public override string ItemId      => "squirrel_meat";
    public override string DisplayName => "Squirrel Meat";
    public override string Article => "some";
    public override string Description => "A small dressed haunch of squirrel meat";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(45).Add<FatHumor>(30).Add<SaltHumor>(25);
}
