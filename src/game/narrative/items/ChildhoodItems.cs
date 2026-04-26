using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

// ──────────────────────────────────────────────────────────────────────────────
// Items granted by REMEMBER actions during the childhood reminescence phase.
// Clothing types from the design draft are split into multiple pieces (a
// "noble clothing" outcome materialises as silk stockings, a knee-length coat,
// a noble undertunic, etc.) so the protagonist's anchors fill in a coherent way.
// ──────────────────────────────────────────────────────────────────────────────

// ── Stable child clothes ──────────────────────────────────────────────
public sealed class StableChildSmock : Item
{
    public override string ItemId           => "stable_child_smock";
    public override string DisplayName      => "Stable Child Smock";
    public override string Description      => "A coarse linen smock stained with hay-dust and oat husks";
    public override ItemSize Size           => ItemSize.Medium;
    public override List<ItemType> Types    => new() { ItemType.Bodywear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Bodywear;
}

public sealed class StableChildBreeches : Item
{
    public override string ItemId           => "stable_child_breeches";
    public override string DisplayName      => "Stable Child Breeches";
    public override string Description      => "Knee-length wool breeches patched at the seat";
    public override List<ItemType> Types    => new() { ItemType.Legwear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Legwear;
}

public sealed class StableChildClogs : Item
{
    public override string ItemId           => "stable_child_clogs";
    public override string DisplayName      => "Wooden Clogs";
    public override string Description      => "Heavy wooden clogs, soled with old straw padding";
    public override List<ItemType> Types    => new() { ItemType.Footwear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Footwear;
}

// ── Townsman clothes ──────────────────────────────────────────────────
public sealed class TownsmanCloak : Item
{
    public override string ItemId           => "townsman_cloak";
    public override string DisplayName      => "Townsman Cloak";
    public override string Description      => "A serviceable hooded cloak of plain dark wool";
    public override ItemSize Size           => ItemSize.Medium;
    public override List<ItemType> Types    => new() { ItemType.Outerwear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Outerwear;
}

public sealed class TownsmanTunic : Item
{
    public override string ItemId           => "townsman_tunic";
    public override string DisplayName      => "Townsman Tunic";
    public override string Description      => "A plain belted tunic of undyed linen";
    public override ItemSize Size           => ItemSize.Medium;
    public override List<ItemType> Types    => new() { ItemType.Bodywear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Bodywear;
}

public sealed class TownsmanBreeches : Item
{
    public override string ItemId           => "townsman_breeches";
    public override string DisplayName      => "Townsman Breeches";
    public override string Description      => "Knee-length grey breeches of close-woven wool";
    public override List<ItemType> Types    => new() { ItemType.Legwear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Legwear;
}

public sealed class TownsmanCap : Item
{
    public override string ItemId           => "townsman_cap";
    public override string DisplayName      => "Townsman Cap";
    public override string Description      => "A felt cap with a turned-up brim";
    public override List<ItemType> Types    => new() { ItemType.Headgear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Headgear;
}

// ── Plain robe (orphanage / temple) ───────────────────────────────────
public sealed class PlainRobe : Item
{
    public override string ItemId           => "plain_robe";
    public override string DisplayName      => "Plain Robe";
    public override string Description      => "A long undyed wool robe with a knotted cord at the waist";
    public override ItemSize Size           => ItemSize.Large;
    public override List<ItemType> Types    => new() { ItemType.Outerwear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Outerwear;
}

// ── Farmer clothing ───────────────────────────────────────────────────
public sealed class FarmerSmock : Item
{
    public override string ItemId           => "farmer_smock";
    public override string DisplayName      => "Farmer Smock";
    public override string Description      => "A heavy linen smock smelling faintly of grain and barn";
    public override ItemSize Size           => ItemSize.Medium;
    public override List<ItemType> Types    => new() { ItemType.Bodywear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Bodywear;
}

public sealed class FarmerBreeches : Item
{
    public override string ItemId           => "farmer_breeches";
    public override string DisplayName      => "Farmer Breeches";
    public override string Description      => "Sturdy wool breeches, knee-tied with leather thongs";
    public override List<ItemType> Types    => new() { ItemType.Legwear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Legwear;
}

public sealed class FarmerStrawHat : Item
{
    public override string ItemId           => "farmer_straw_hat";
    public override string DisplayName      => "Straw Hat";
    public override string Description      => "A wide-brimmed straw hat, bleached pale by sun";
    public override List<ItemType> Types    => new() { ItemType.Headgear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Headgear;
}

public sealed class FarmerClogs : Item
{
    public override string ItemId           => "farmer_clogs";
    public override string DisplayName      => "Farmer Clogs";
    public override string Description      => "Caked wooden clogs, heavy and serviceable";
    public override List<ItemType> Types    => new() { ItemType.Footwear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Footwear;
}

// ── Noble clothing ────────────────────────────────────────────────────
public sealed class SilkStockings : Item
{
    public override string ItemId           => "silk_stockings";
    public override string DisplayName      => "Silk Stockings";
    public override string Description      => "A pair of pale-grey silk stockings, finely knitted";
    public override List<ItemType> Types    => new() { ItemType.Legwear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Legwear;
}

public sealed class KneeLengthCoat : Item
{
    public override string ItemId           => "knee_length_coat";
    public override string DisplayName      => "Knee-length Coat";
    public override string Description      => "A panelled coat of dark wool trimmed in velvet";
    public override ItemSize Size           => ItemSize.Large;
    public override List<ItemType> Types    => new() { ItemType.Outerwear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Outerwear;
}

public sealed class NobleUndertunic : Item
{
    public override string ItemId           => "noble_undertunic";
    public override string DisplayName      => "Noble Undertunic";
    public override string Description      => "A fine ivory linen undertunic with embroidered cuffs";
    public override ItemSize Size           => ItemSize.Medium;
    public override List<ItemType> Types    => new() { ItemType.Bodywear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Bodywear;
}

public sealed class SoftLeatherShoes : Item
{
    public override string ItemId           => "soft_leather_shoes";
    public override string DisplayName      => "Soft Leather Shoes";
    public override string Description      => "Thin-soled shoes of supple dyed leather, not made for rough roads";
    public override List<ItemType> Types    => new() { ItemType.Footwear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Footwear;
}

// ── Travelling supplies (curiosity / dream / gold_thirst) ─────────────
public sealed class TravelersBackpack : ContainerItem
{
    public override string ItemId           => "travelers_backpack";
    public override string DisplayName      => "Travellers' Backpack";
    public override string Description      => "A heavy canvas backpack with leather straps, sized for the road";
    public override ItemSize Size           => ItemSize.Large;
    public override int    ContentSlots     => 18;
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.BeltGear;
}

public sealed class Sausage : Item
{
    public override string ItemId      => "sausage";
    public override string DisplayName => "Sausage";
    public override string Description => "A coil of cured pork sausage, dark and pungent";
}

public sealed class LeatherCanteen : BottleItem
{
    public override string ItemId           => "leather_canteen";
    public override string DisplayName      => "Leather Canteen";
    public override string Description      => "A wax-treated leather canteen on a long shoulder strap";
    public override ItemSize Size           => ItemSize.Small;
    public override int    ContentSlots     => 3;
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.BeltGear;
}

public sealed class WaterDraught : Item
{
    public override string ItemId      => "water_draught";
    public override string DisplayName => "Water";
    public override string Description => "Cool fresh water";
    public override List<ItemType> Types => new() { ItemType.Liquid };
}

// ── Sundries ──────────────────────────────────────────────────────────
public sealed class Hairpin : Item
{
    public override string ItemId      => "hairpin";
    public override string DisplayName => "Hairpin";
    public override string Description => "A slim brass hairpin, bent into a useful pick";
    public override int    UsageLevel  => 2;
}

public sealed class GoldCoin : Item
{
    public override string ItemId      => "gold_coin";
    public override string DisplayName => "Gold Coin";
    public override string Description => "A single bright gold coin, milled and weighty";
}

public sealed class SilverCoin : Item
{
    public override string ItemId      => "silver_coin";
    public override string DisplayName => "Silver Coin";
    public override string Description => "A single tarnished silver coin";
}

public sealed class ShortSword : Item
{
    public override string ItemId           => "short_sword";
    public override string DisplayName      => "Short Sword";
    public override string Description      => "A plain straight-bladed short sword in a worn leather scabbard";
    public override ItemSize Size           => ItemSize.Medium;
    public override int    UsageLevel       => 4;
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.RightHold;
}

public sealed class WoodenStick : Item
{
    public override string ItemId      => "wooden_stick";
    public override string DisplayName => "Wooden Stick";
    public override string Description => "A weather-greyed stick still half-imagined as a magic sword";
    public override int    UsageLevel  => 1;
}

public sealed class WoodenDoll : Item
{
    public override string ItemId      => "wooden_doll";
    public override string DisplayName => "Wooden Doll";
    public override string Description => "A small, lovingly worn doll of carved oak — a sleeping princess once";
}

public sealed class Worm : Item
{
    public override string ItemId      => "worm";
    public override string DisplayName => "Worm";
    public override string Description => "A long pale earthworm, still squirming";
}

public sealed class MouseMeat : Item
{
    public override string ItemId      => "mouse_meat";
    public override string DisplayName => "Mouse Meat";
    public override string Description => "A scrap of stringy mouse flesh, scarcely a mouthful";
}

public sealed class SquirrelMeat : Item
{
    public override string ItemId      => "squirrel_meat";
    public override string DisplayName => "Squirrel Meat";
    public override string Description => "A small dressed haunch of squirrel meat";
}
