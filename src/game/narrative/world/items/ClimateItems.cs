using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

// ─────────────────────────────────────────────────────────────────────────────
//  What grows, lies and swims in the hot and cold country (see ClimateRule). Built on the same
//  bases as the temperate catalogue, so a date is a fruit and a block of sandstone is stone to
//  every rule that asks — nothing here is a kind of thing the game did not already have, only a
//  thing of that kind it did not have.
// ─────────────────────────────────────────────────────────────────────────────

// ── Fruit ─────────────────────────────────────────────────────────────────────

public sealed class Date : FruitItem
{
    public override string ItemId      => "date";
    public override string DisplayName => "Date";
    public override string Description => "A wrinkled amber date, sticky and very sweet, the stone loose inside";
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(60).Add<PulpHumor>(25).Add<FiberHumor>(15);
}

public sealed class PricklyPear : FruitItem
{
    public override string ItemId      => "prickly_pear";
    public override string DisplayName => "Prickly Pear";
    public override string Description => "A red cactus fruit furred with fine spines that find the fingers whatever care is taken";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AquaHumor>(40).Add<SugarHumor>(35).Add<PulpHumor>(25);
}

public sealed class BaobabFruit : FruitItem
{
    public override string ItemId      => "baobab_fruit";
    public override string DisplayName => "Baobab Fruit";
    public override string Description => "A velvet-skinned gourd of a fruit, its pith dry and chalky and sour";
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PulpHumor>(45).Add<CalxHumor>(30).Add<FiberHumor>(25);
}

public sealed class Banana : FruitItem
{
    public override string ItemId      => "banana";
    public override string DisplayName => "Banana";
    public override string Description => "A short wild banana, thick-skinned and full of hard black seeds";
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(45).Add<PulpHumor>(45).Add<FiberHumor>(10);
}

public sealed class JungleFig : FruitItem
{
    public override string ItemId      => "jungle_fig";
    public override string DisplayName => "Jungle Fig";
    public override string Description => "A soft purple fig from a strangler's crown, wasps already at the split in it";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(50).Add<PulpHumor>(35).Add<FiberHumor>(15);
}

public sealed class CacaoPod : FruitItem
{
    public override string ItemId      => "cacao_pod";
    public override string DisplayName => "Cacao Pod";
    public override string Description => "A ridged yellow pod off the trunk itself, white pulp and bitter beans inside";
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(35).Add<EuphoraHumor>(35).Add<SugarHumor>(30);
}

public sealed class PinyonNut : FruitItem
{
    public override string ItemId      => "pinyon_nut";
    public override string DisplayName => "Pinyon Nut";
    public override string Description => "A small brown pine nut, oily and resin-sweet, prised out of a squat cone";
    public override bool IsHard => true;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(55).Add<FiberHumor>(30).Add<SugarHumor>(15);
}

public sealed class JuniperBerry : FruitItem
{
    public override string ItemId      => "juniper_berry";
    public override string DisplayName => "Juniper Berry";
    public override string Description => "A dusty blue juniper berry, sharp and piney on the tongue";
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<VaporHumor>(45).Add<FiberHumor>(30).Add<SugarHumor>(25);
}

public sealed class Cloudberry : FruitItem
{
    public override string ItemId      => "cloudberry";
    public override string DisplayName => "Cloudberry";
    public override string Description => "A soft amber cloudberry, like a raspberry gone gold in the cold";
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(45).Add<PulpHumor>(35).Add<AquaHumor>(20);
}

public sealed class Crowberry : FruitItem
{
    public override string ItemId      => "crowberry";
    public override string DisplayName => "Crowberry";
    public override string Description => "A glossy black crowberry off a mat of needle-leaved heath, watery and faintly bitter";
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AquaHumor>(40).Add<FiberHumor>(35).Add<SugarHumor>(25);
}

// ── Roots and grain ───────────────────────────────────────────────────────────

public sealed class YuccaRoot : VegetableItem
{
    public override string ItemId      => "yucca_root";
    public override string DisplayName => "Yucca Root";
    public override string Description => "A pale woody root dug from under a yucca, soapy when wet and starchy when cooked";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PulpHumor>(50).Add<FiberHumor>(35).Add<CalxHumor>(15);
}

public sealed class WildMillet : VegetableItem
{
    public override string ItemId      => "wild_millet";
    public override string DisplayName => "Wild Millet";
    public override string Description => "A drooping head of wild millet, the seed small and hard and easily shaken loose";
    public override WeightClass Weight => WeightClass.Insignificant;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PulpHumor>(55).Add<FiberHumor>(35).Add<SugarHumor>(10);
}

// ── Herbs, resins and gums ────────────────────────────────────────────────────

public sealed class Myrrh : HerbItem
{
    public override string ItemId      => "myrrh";
    public override string DisplayName => "Myrrh";
    public override string Description => "A tear of red-brown resin bled from a thorny desert shrub, bitter and fragrant";
    public override int PriceReference => 12;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<EtherHumor>(45).Add<OpiumHumor>(30).Add<VaporHumor>(25);
}

public sealed class AloeLeaf : HerbItem
{
    public override string ItemId      => "aloe_leaf";
    public override string DisplayName => "Aloe Leaf";
    public override string Description => "A thick toothed aloe leaf that weeps a cool clear gel where it is broken";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PhlegmHumor>(50).Add<AquaHumor>(30).Add<YellowBileHumor>(20);
}

public sealed class AcaciaGum : HerbItem
{
    public override string ItemId      => "acacia_gum";
    public override string DisplayName => "Acacia Gum";
    public override string Description => "A hard clear bead of gum from a split in an acacia's bark, tasteless and slow to melt";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PhlegmHumor>(45).Add<SugarHumor>(35).Add<FiberHumor>(20);
}

public sealed class Copal : HerbItem
{
    public override string ItemId      => "copal";
    public override string DisplayName => "Copal";
    public override string Description => "A lump of pale jungle resin; burnt, it gives a white smoke heavy as incense";
    public override int PriceReference => 9;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<EtherHumor>(50).Add<FumeHumor>(30).Add<EuphoraHumor>(20);
}

public sealed class Peppercorn : HerbItem
{
    public override string ItemId      => "peppercorn";
    public override string DisplayName => "Peppercorn";
    public override string Description => "A string of red wild peppercorns off a climbing vine, hot enough to water the eyes";
    public override int PriceReference => 8;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<CholerHumor>(55).Add<VaporHumor>(30).Add<SulfurHumor>(15);
}

public sealed class Orchid : HerbItem
{
    public override string ItemId      => "orchid";
    public override string DisplayName => "Orchid";
    public override string Description => "A waxy orchid flower, speckled like a moth's wing, its scent strongest at dusk";
    public override int PriceReference => 6;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<VoluptasHumor>(45).Add<EtherHumor>(35).Add<EuphoraHumor>(20);
}

public sealed class LabradorTea : HerbItem
{
    public override string ItemId      => "labrador_tea";
    public override string DisplayName => "Labrador Tea";
    public override string Description => "A sprig of leathery leaves rusty-furred underneath, resinous and medicinal";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<VaporHumor>(45).Add<OpiumHumor>(30).Add<EtherHumor>(25);
}

// ── Plants for working ────────────────────────────────────────────────────────

public sealed class ReindeerMoss : ConsumableItem
{
    public override string ItemId      => "reindeer_moss";
    public override string DisplayName => "Reindeer Moss";
    public override string Description => "A spongy grey-white tuft of lichen, branched like antler, crisp in the cold";
    public override List<ItemTag> Tags => new() { ItemTag.Forage };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 2;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(50).Add<FungiHumor>(35).Add<BlackBileHumor>(15);
}

public sealed class CottonGrass : TextileItem
{
    public override string ItemId      => "cotton_grass";
    public override string DisplayName => "Cotton Grass";
    public override string Description => "A fistful of cotton-grass heads, white tufts that make a poor thread and a good tinder";
    public override WeightClass Weight => WeightClass.Insignificant;
    public override int PriceReference => 3;
}

public sealed class Liana : WoodRawItem
{
    public override string ItemId      => "liana";
    public override string DisplayName => "Liana";
    public override string Description => "A length of woody jungle vine, tough as rope and nearly as long";
    public override List<ItemTag> Tags => new() { ItemTag.Wood, ItemTag.Forage };
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 3;
}

public sealed class Bamboo : WoodRawItem
{
    public override string ItemId      => "bamboo";
    public override string DisplayName => "Bamboo";
    public override string Description => "A jointed cane of green bamboo, hollow, light and very hard";
    public override ItemSize Size => ItemSize.Large;
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 4;
}

public sealed class Thornwood : WoodRawItem
{
    public override string ItemId      => "thornwood";
    public override string DisplayName => "Thornwood";
    public override string Description => "A crooked bough of acacia, dense red wood bristling with long white thorns";
    public override int PriceReference => 4;
}

public sealed class PalmFrond : WoodRawItem
{
    public override string ItemId      => "palm_frond";
    public override string DisplayName => "Palm Frond";
    public override string Description => "A great fan of dried palm leaf, good for thatch, for shade and for weaving";
    public override ItemSize Size => ItemSize.Large;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Wood, ItemTag.Forage };
    public override int PriceReference => 2;
}

// ── Earths and stone ──────────────────────────────────────────────────────────

public sealed class Natron : StoneRawItem
{
    public override string ItemId      => "natron";
    public override string DisplayName => "Natron";
    public override string Description => "A crust of white salt scraped off a dry lake bed, soapy to the touch and bitter";
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 7;
}

public sealed class DesertRose : StoneRawItem
{
    public override string ItemId      => "desert_rose";
    public override string DisplayName => "Desert Rose";
    public override string Description => "A cluster of sand-coloured crystal blades grown together like the petals of a rose";
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 14;
}

public sealed class Ochre : StoneRawItem
{
    public override string ItemId      => "ochre";
    public override string DisplayName => "Ochre";
    public override string Description => "A lump of soft red earth that leaves its colour on everything it touches";
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 6;
}

public sealed class Sandstone : StoneRawItem
{
    public override string ItemId      => "sandstone";
    public override string DisplayName => "Sandstone";
    public override string Description => "A banded block of red sandstone, soft enough to carve and gritty under the thumb";
    public override int PriceReference => 4;
}

public sealed class Jasper : StoneRawItem
{
    public override string ItemId      => "jasper";
    public override string DisplayName => "Jasper";
    public override string Description => "A water-polished pebble of blood-red jasper, opaque and cold";
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 12;
}

public sealed class RockFlour : StoneRawItem
{
    public override string ItemId      => "rock_flour";
    public override string DisplayName => "Rock Flour";
    public override string Description => "A pinch of grey stone ground finer than meal under the weight of the ice";
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 2;
}

/// <summary>Ice to suck on. Food rather than drink, so it is picked up in the hand like a stone.</summary>
public sealed class IceShard : ConsumableItem
{
    public override string ItemId      => "ice_shard";
    public override string DisplayName => "Ice Shard";
    public override string Description => "A clear wedge of old ice, blue at its heart, numbing the hand that holds it";
    public override List<ItemTag> Tags => new() { ItemTag.Mineral };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 1;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AquaHumor>(80).Add<PhlegmHumor>(20);
}

// ── Fish ──────────────────────────────────────────────────────────────────────

public sealed class Catfish : SeaFoodItem
{
    public override string ItemId      => "catfish";
    public override string DisplayName => "Catfish";
    public override string Description => "A whiskered brown river fish, slimy and broad-headed, muddy in the smell";
    public override int    PriceReference => 5;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(45).Add<BloodHumor>(35).Add<PhlegmHumor>(20);
}

public sealed class ArcticChar : SeaFoodItem
{
    public override string ItemId      => "arctic_char";
    public override string DisplayName => "Arctic Char";
    public override string Description => "A red-bellied char out of water barely above freezing, firm and rich";
    public override int    PriceReference => 7;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(45).Add<BloodHumor>(40).Add<SaltHumor>(15);
}
