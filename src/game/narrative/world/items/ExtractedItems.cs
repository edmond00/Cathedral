using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

// ── The one missing tool ──────────────────────────────────────────────────────

/// <summary>
/// A fishing rod. The <c>Hook</c> and <c>FishingLine</c> already existed as trade goods; nothing put
/// them on a pole, so there was no way to fish.
/// </summary>
public sealed class FishingRod : ToolItem
{
    public override string ItemId      => "fishing_rod";
    public override string DisplayName => "Fishing Rod";
    public override string Description => "A limber cut pole with line whipped to its tip and a hook set into the cork";
    public override WeightClass Weight => WeightClass.Light;
    public override int   UsageLevel   => 5;
    public override int   PriceReference => 14;
}

// ── Freshwater fish ───────────────────────────────────────────────────────────
// Herring, cod and mackerel already existed for the coast. Inland water had nothing in it.

public sealed class Trout : SeaFoodItem
{
    public override string ItemId      => "trout";
    public override string DisplayName => "Trout";
    public override string Description => "A speckled fish still stiff from the water, cold and heavy for its length";
    public override int    PriceReference => 6;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(45).Add<FatHumor>(35).Add<SaltHumor>(20);
}

public sealed class Eel : SeaFoodItem
{
    public override string ItemId      => "eel";
    public override string DisplayName => "Eel";
    public override string Description => "A long muscular eel that will not stop moving even after it should have";
    public override int    PriceReference => 7;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(50).Add<BloodHumor>(35).Add<SaltHumor>(15);
}

public sealed class Pike : SeaFoodItem
{
    public override string ItemId      => "pike";
    public override string DisplayName => "Pike";
    public override string Description => "A lean predator with a jaw of backward teeth, taken out of the weed";
    public override WeightClass Weight => WeightClass.Medium;
    public override int    PriceReference => 9;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(60).Add<FatHumor>(25).Add<SaltHumor>(15);
}

public sealed class Perch : SeaFoodItem
{
    public override string ItemId      => "perch";
    public override string DisplayName => "Perch";
    public override string Description => "A barred green fish with a spined back fin, small and sweet";
    public override int    PriceReference => 5;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(50).Add<FatHumor>(30).Add<SaltHumor>(20);
}

// ── Ores ──────────────────────────────────────────────────────────────────────
// Iron ore and coal already existed. A mine that only ever gives up iron is one mine.

public sealed class CopperOre : StoneRawItem
{
    public override string ItemId      => "copper_ore";
    public override string DisplayName => "Copper Ore";
    public override string Description => "Rock shot through with green and rust-red, heavier than it looks";
    public override int    PriceReference => 9;
}

public sealed class TinOre : StoneRawItem
{
    public override string ItemId      => "tin_ore";
    public override string DisplayName => "Tin Ore";
    public override string Description => "Dull dark stone with a dead weight to it, broken out of a narrow seam";
    public override int    PriceReference => 11;
}

public sealed class LeadOre : StoneRawItem
{
    public override string ItemId      => "lead_ore";
    public override string DisplayName => "Lead Ore";
    public override string Description => "Grey crystalline rock that breaks in cubes and marks the hand";
    public override WeightClass Weight => WeightClass.Heavy;
    public override int    PriceReference => 8;
}

// ── Earths ────────────────────────────────────────────────────────────────────

public sealed class Sand : StoneRawItem
{
    public override string ItemId      => "sand";
    public override string DisplayName => "Sand";
    public override string Description => "Coarse pale sand, dry enough to run through the fingers";
    public override WeightClass Weight => WeightClass.Light;
    public override int    PriceReference => 2;
}

public sealed class Peat : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "peat";
    public override string DisplayName => "Peat";
    public override string Description => "A cut block of black peat, wet through and smelling of the ground it came out of";
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Mineral };
    public override int    PriceReference => 3;
}

public sealed class Loam : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "loam";
    public override string DisplayName => "Loam";
    public override string Description => "Dark crumbling earth with the roots still in it, good ground by any measure";
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Mineral };
    public override int    PriceReference => 2;
}

// ── Worked wood ───────────────────────────────────────────────────────────────
// Log, Plank and Twig existed. What was missing was the middle of the range.

public sealed class Cordwood : WoodRawItem
{
    public override string ItemId      => "cordwood";
    public override string DisplayName => "Cordwood";
    public override string Description => "Split lengths cut to a arm's span, stacked and half-seasoned";
    public override int    PriceReference => 4;
}

public sealed class Beam : WoodRawItem
{
    public override string ItemId      => "beam";
    public override string DisplayName => "Beam";
    public override string Description => "A squared baulk of timber, adzed flat on all four faces and heavy as a man";
    public override WeightClass Weight => WeightClass.Heavy;
    public override int    PriceReference => 12;
}

// ── Salvage ───────────────────────────────────────────────────────────────────

/// <summary>What is left when furniture is broken up on purpose.</summary>
public sealed class SplinteredTimber : WoodRawItem
{
    public override string ItemId      => "splintered_timber";
    public override string DisplayName => "Splintered Timber";
    public override string Description => "Broken boards with the nails still in them, sprung apart along the grain";
    public override WeightClass Weight => WeightClass.Light;
    public override int    PriceReference => 1;
}
