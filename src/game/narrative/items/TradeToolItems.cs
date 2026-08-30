using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

// The working tools of the village trades — what an NPC of each archetype is actually holding or has
// hanging at their belt. Grouped in one file (like WeaponItems.cs) because each is a handful of lines
// and they are only ever read as a set, by NpcContentGenerator via the archetype loadouts.
//
// UsageLevel is the bonus-dice contribution when the tool is combined with an action, so it tracks how
// specialised the tool is: a whetstone helps a little with anything, a mill-pick helps enormously with
// dressing a millstone and with nothing else.

// ── Forge ──────────────────────────────────────────────────────────────────────

public sealed class SmithTongs : Item
{
    public override string ItemId      => "smith_tongs";
    public override string DisplayName => "Smith's Tongs";
    public override string Description => "Long iron tongs, jaws blackened and slightly splayed from gripping white-hot bar";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass Weight       => WeightClass.Medium;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 5;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 20;
}

/// <summary>
/// An apron goes over the clothes, so it is Outerwear — the old declaration claimed Bodywear while
/// preferring the Outerwear anchor, a contradiction that meant it could never occupy its own
/// preferred slot and always fell through to the anchor scan.
/// </summary>
public sealed class BlacksmithApron : WearableItem
{
    public override string ItemId           => "blacksmith_apron";
    public override string DisplayName      => "Scorched Leather Apron";
    public override string Description      => "A heavy leather apron burned through in a constellation of small holes";
    public override ItemSize Size           => ItemSize.Medium;
    public override WearSlot Slot           => WearSlot.Outerwear;
    public override List<ItemTag> Tags      => new() { ItemTag.Clothing };
    public override int PriceReference      => 20;

    // Heavy hide meant to stop sparks and scale — it stops a little more than that.
    public override int DefenseDice => 1;
    public override IReadOnlyList<Npc.SocialCategory> DialogueAppeal =>
        new[] { Npc.SocialCategory.Bourgeois };
}

// ── Wood ───────────────────────────────────────────────────────────────────────

public sealed class CarpentersPlane : Item
{
    public override string ItemId      => "carpenters_plane";
    public override string DisplayName => "Carpenter's Plane";
    public override string Description => "A beechwood plane with an iron blade, the sole polished glassy by use";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass Weight       => WeightClass.Medium;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 5;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Craftware };
    public override int PriceReference => 20;
}

public sealed class WoodChisel : Item
{
    public override string ItemId      => "wood_chisel";
    public override string DisplayName => "Wood Chisel";
    public override string Description => "A flat iron chisel with a mallet-bruised wooden handle";
    public override ItemSize Size      => ItemSize.Small;
    public override WeightClass Weight       => WeightClass.Light;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 8;
}

public sealed class Drawknife : Item
{
    public override string ItemId      => "drawknife";
    public override string DisplayName => "Drawknife";
    public override string Description => "A two-handled blade for shaving staves down to a curve";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass Weight       => WeightClass.Light;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 5;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 10;
}

public sealed class WoodenMallet : Item
{
    public override string ItemId      => "wooden_mallet";
    public override string DisplayName => "Wooden Mallet";
    public override string Description => "A one-piece beech mallet, the face dented all round";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass Weight       => WeightClass.Medium;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Wood };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 5;
}

// ── Cloth and fleece ───────────────────────────────────────────────────────────

public sealed class Spindle : Item
{
    public override string ItemId      => "spindle";
    public override string DisplayName => "Drop Spindle";
    public override string Description => "A weighted wooden spindle, thread still wound half up the shaft";
    public override ItemSize Size      => ItemSize.Small;
    public override WeightClass Weight       => WeightClass.Light;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Textile };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 6;
}

public sealed class WoolShears : Item
{
    public override string ItemId      => "wool_shears";
    public override string DisplayName => "Wool Shears";
    public override string Description => "Spring shears of a single bent piece of iron, ground thin at the blades";
    public override ItemSize Size      => ItemSize.Small;
    public override WeightClass Weight       => WeightClass.Light;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 9;
}

public sealed class BoneNeedle : Item
{
    public override string ItemId      => "bone_needle";
    public override string DisplayName => "Bone Needle";
    public override string Description => "A polished bone needle carried pushed through a scrap of cloth";
    public override ItemSize Size      => ItemSize.Small;
    public override WeightClass Weight       => WeightClass.Insignificant;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Craftware };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 2;
}

// ── Field and farm ─────────────────────────────────────────────────────────────

public sealed class ShepherdsCrook : Item
{
    public override string ItemId      => "shepherds_crook";
    public override string DisplayName => "Shepherd's Crook";
    public override string Description => "A long ash staff with a worn horn hook, smoothed dark where the hand sits";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass Weight       => WeightClass.Medium;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Wood };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 7;
}

public sealed class TallyStick : Item
{
    public override string ItemId      => "tally_stick";
    public override string DisplayName => "Tally Stick";
    public override string Description => "A hazel rod notched down one edge, the count of a season cut into it";
    public override ItemSize Size      => ItemSize.Small;
    public override WeightClass Weight       => WeightClass.Light;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Wood };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 1;
}

/// <summary>A pail's whole purpose is what is in it � a vessel, not a tool you swing.</summary>
public sealed class MilkPail : ContainerItem
{
    public override string ItemId      => "milk_pail";
    public override string DisplayName => "Milk Pail";
    public override string Description => "A staved wooden pail, the inside scoured pale and smelling faintly sour";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass Weight       => WeightClass.Medium;
    public override ContainerKind Kind => ContainerKind.Vessel;
    public override int ContentSlots   => 6;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Craftware };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 6;
}

public sealed class ChurnPaddle : Item
{
    public override string ItemId      => "churn_paddle";
    public override string DisplayName => "Churn Paddle";
    public override string Description => "A long-handled paddle worn thin on one side from a lifetime of the same stroke";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass Weight       => WeightClass.Light;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Wood };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 4;
}

public sealed class WickerBasket : ContainerItem
{
    public override string ItemId      => "wicker_basket";
    public override string DisplayName => "Wicker Basket";
    public override string Description => "A shallow willow basket lined with straw, mended in two places with cord";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass Weight       => WeightClass.Light;
    public override ContainerKind Kind => ContainerKind.Storage;
    public override int ContentSlots   => 9;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 4;
}

public sealed class DroversSwitch : Item
{
    public override string ItemId      => "drovers_switch";
    public override string DisplayName => "Drover's Switch";
    public override string Description => "A whippy hazel switch, stripped of bark and split at the tip";
    public override ItemSize Size      => ItemSize.Small;
    public override WeightClass Weight       => WeightClass.Insignificant;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 2;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Wood };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 1;
}

public sealed class Flail : Item
{
    public override string ItemId      => "flail";
    public override string DisplayName => "Threshing Flail";
    public override string Description => "Two lengths of ash joined by a leather thong, both ends beaten smooth";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass Weight       => WeightClass.Medium;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Wood };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 8;
}

public sealed class Hoe : Item
{
    public override string ItemId      => "hoe";
    public override string DisplayName => "Hoe";
    public override string Description => "A long-hafted hoe, the blade worn to a crescent by stony ground";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass Weight       => WeightClass.Medium;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 10;
}

// ── Mill, oven and mash ────────────────────────────────────────────────────────

public sealed class MillPick : Item
{
    public override string ItemId      => "mill_pick";
    public override string DisplayName => "Mill Pick";
    public override string Description => "A short double-ended pick for cutting the grooves back into a millstone";
    public override ItemSize Size      => ItemSize.Small;
    public override WeightClass Weight       => WeightClass.Light;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 5;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 20;
}

public sealed class BakersPeel : Item
{
    public override string ItemId      => "bakers_peel";
    public override string DisplayName => "Baker's Peel";
    public override string Description => "A long flat wooden shovel, the blade scorched black along its leading edge";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass Weight       => WeightClass.Medium;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Wood };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 7;
}

public sealed class MashPaddle : Item
{
    public override string ItemId      => "mash_paddle";
    public override string DisplayName => "Mash Paddle";
    public override string Description => "A broad oak paddle stained dark to the handle by years of wort";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass Weight       => WeightClass.Medium;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Wood };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 6;
}

// ── Water, wood and rock ───────────────────────────────────────────────────────

public sealed class FishingNet : Item
{
    public override string ItemId      => "fishing_net";
    public override string DisplayName => "Fishing Net";
    public override string Description => "A hand-knotted net with cork floats, smelling permanently of the water";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass Weight       => WeightClass.Heavy;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 5;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Craftware };
    public override int PriceReference => 20;
}

public sealed class FishHooks : Item
{
    public override string ItemId      => "fish_hooks";
    public override string DisplayName => "Bone Fish-Hooks";
    public override string Description => "A twist of line holding a half-dozen barbed bone hooks";
    public override ItemSize Size      => ItemSize.Small;
    public override WeightClass Weight       => WeightClass.Insignificant;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Craftware };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 3;
}

public sealed class CharcoalRake : Item
{
    public override string ItemId      => "charcoal_rake";
    public override string DisplayName => "Charcoal Rake";
    public override string Description => "A long iron rake for drawing coal out of a cooling clamp, permanently sooted";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass Weight       => WeightClass.Medium;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 9;
}

public sealed class MinersLamp : Item
{
    public override string ItemId      => "miners_lamp";
    public override string DisplayName => "Miner's Lamp";
    public override string Description => "A small iron oil lamp with a hooked handle, the glass smoked half-opaque";
    public override ItemSize Size      => ItemSize.Small;
    public override WeightClass Weight       => WeightClass.Light;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 10;
}

public sealed class Whetstone : Item
{
    public override string ItemId      => "whetstone";
    public override string DisplayName => "Whetstone";
    public override string Description => "A pocket whetstone worn into a shallow saddle down the middle";
    public override ItemSize Size      => ItemSize.Small;
    public override WeightClass Weight       => WeightClass.Light;
    public override ItemCategory Category => ItemCategory.Tool;
    public override int   UsageLevel   => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Mineral };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 3;
}
