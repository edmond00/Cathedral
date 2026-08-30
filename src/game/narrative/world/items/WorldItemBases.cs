using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

// ── Foraged / produce ───────────────────────────────────────────────────────

/// <summary>Edible orchard or wild fruit. Small, light, perishable.</summary>
public abstract class FruitItem : ConsumableItem
{
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass    Weight => WeightClass.Insignificant;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    // Small wild fruit and berries default to a light composition; tree fruit overrides to Hearty.
    protected override HumorRichness Richness => HumorRichness.Modest;

    public override List<ItemTag> Tags    => new() { ItemTag.Crop };
    public override CoinType      PriceCoin    => CoinType.Copper;
    public override int           PriceReference => 3;
}

/// <summary>Edible root vegetable or pod. Small, light, edible raw.</summary>
public abstract class VegetableItem : ConsumableItem
{
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass    Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    // Cultivated vegetables are proper food.
    protected override HumorRichness Richness => HumorRichness.Hearty;

    public override List<ItemTag> Tags    => new() { ItemTag.Crop };
    public override CoinType      PriceCoin    => CoinType.Copper;
    public override int           PriceReference => 4;
}

/// <summary>Foraged herb sprig. Very light, used for flavour/medicine.</summary>
public abstract class HerbItem : ConsumableItem
{
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass    Weight => WeightClass.Insignificant;
    public override ConsumableType ConsumableType => ConsumableType.Inhalant;
    // Inhaled sprigs carry only a wisp of humor.
    protected override HumorRichness Richness => HumorRichness.Sparse;

    public override List<ItemTag> Tags    => new() { ItemTag.Herb };
    public override CoinType      PriceCoin    => CoinType.Copper;
    public override int           PriceReference => 2;
}

// ── Raw materials ───────────────────────────────────────────────────────────

/// <summary>Cut or fallen wood — log, plank, twig, sap, etc.</summary>
public abstract class WoodRawItem : Item
{
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass    Weight => WeightClass.Medium;
    public override ItemCategory Category => ItemCategory.Crafting;

    public override List<ItemTag> Tags    => new() { ItemTag.Wood };
    public override CoinType      PriceCoin    => CoinType.Copper;
    public override int           PriceReference => 5;
}

/// <summary>Stone, clay and other earthy raw material.</summary>
public abstract class StoneRawItem : Item
{
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass    Weight => WeightClass.Medium;
    public override ItemCategory Category => ItemCategory.Crafting;

    public override List<ItemTag> Tags    => new() { ItemTag.Mineral };
    public override CoinType      PriceCoin    => CoinType.Copper;
    public override int           PriceReference => 6;
}

/// <summary>Smelted or raw metal goods. Heavy for their volume.</summary>
public abstract class MetalItem : Item
{
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass    Weight => WeightClass.Medium;
    public override ItemCategory Category => ItemCategory.Crafting;

    public override List<ItemTag> Tags    => new() { ItemTag.Mineral };
    public override CoinType      PriceCoin    => CoinType.Copper;
    public override int           PriceReference => 10;
}

/// <summary>Forged iron tool. Medium, heavy-ish, gives a usage bonus when wielded.</summary>
public abstract class ToolItem : Item
{
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass    Weight => WeightClass.Light;
    public override int      UsageLevel => 4;
    public override ItemCategory Category => ItemCategory.Tool;

    // Forged tools are also part of the blacksmith's Ironwork.
    public override List<ItemTag> Tags    => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int           PriceReference => 20;
}

/// <summary>Spun or woven textile, thread, raw fibre — the material, not the finished garment.</summary>
public abstract class TextileItem : Item
{
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass    Weight => WeightClass.Light;
    public override ItemCategory Category => ItemCategory.Crafting;

    public override List<ItemTag> Tags    => new() { ItemTag.Textile };
    public override CoinType      PriceCoin    => CoinType.Copper;
    public override int           PriceReference => 12;
}

// Hides, pelts, feathers and horns are no longer a base of their own: they are one shared body-part
// vocabulary, and it lives with the rest of what a carcass yields — see BodyPartItem in
// src/game/narrative/items/corpse/BodyPartItems.cs.

/// <summary>Fish, shellfish, seaweed and other sea-edge yields.</summary>
public abstract class SeaFoodItem : ConsumableItem
{
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass    Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    // Fresh-caught fish and shellfish are nourishing.
    protected override HumorRichness Richness => HumorRichness.Hearty;

    public override List<ItemTag> Tags    => new() { ItemTag.Fish };
    public override CoinType      PriceCoin    => CoinType.Copper;
    public override int           PriceReference => 4;
}
