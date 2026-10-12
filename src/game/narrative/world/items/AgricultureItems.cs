using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative.World.Items;

// ─────────────────────────────────────────────────────────────────────────────
//  What the settled country grows. The crops of the agriculture locations, one per crop the location
//  table names, and what their people make of them. Ordinary catalogue items: a field's crop row holds
//  them, a planter sells them, and a pack carries them off like anything else.
// ─────────────────────────────────────────────────────────────────────────────

// ── grain and pulse ─────────────────────────────────────────────────────────

public sealed class Barley : ConsumableItem
{
    public override string ItemId      => "barley";
    public override string DisplayName => "Barley";
    public override string Description => "A bundle of bearded barley ears, the awns long and scratchy";
    public override List<ItemTag> Tags => new() { ItemTag.Crop };
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override bool IsHard => true;
    public override string Article => "some";
    public override int PriceReference => 4;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(55).Add<CalxHumor>(25).Add<PulpHumor>(20);
}

public sealed class Oats : ConsumableItem
{
    public override string ItemId      => "oats";
    public override string DisplayName => "Oats";
    public override string Description => "A loose fistful of pale oats in their papery husks";
    public override List<ItemTag> Tags => new() { ItemTag.Crop };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override bool IsHard => true;
    public override string Article => "some";
    public override int PriceReference => 3;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(60).Add<PulpHumor>(25).Add<CalxHumor>(15);
}

public sealed class Rye : ConsumableItem
{
    public override string ItemId      => "rye";
    public override string DisplayName => "Rye";
    public override string Description => "A bundle of tall rye, the ears long, thin and grey-green";
    public override List<ItemTag> Tags => new() { ItemTag.Crop };
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override bool IsHard => true;
    public override string Article => "some";
    public override int PriceReference => 3;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(60).Add<CalxHumor>(20).Add<BlackBileHumor>(20);
}

public sealed class Rice : ConsumableItem
{
    public override string ItemId      => "rice";
    public override string DisplayName => "Rice";
    public override string Description => "A twist of cloth holding husked rice, the grains small and translucent";
    public override List<ItemTag> Tags => new() { ItemTag.Crop };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override bool IsHard => true;
    public override string Article => "some";
    public override int PriceReference => 5;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PulpHumor>(55).Add<FiberHumor>(25).Add<AquaHumor>(20);
}

public sealed class Maize : ConsumableItem
{
    public override string ItemId      => "maize";
    public override string DisplayName => "Maize Ear";
    public override string Description => "A fat ear of maize in its sheath of dry leaves, silk gone brown at the tip";
    public override List<ItemTag> Tags => new() { ItemTag.Crop };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 3;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(35).Add<PulpHumor>(45).Add<FiberHumor>(20);
}

public sealed class Bean : VegetableItem
{
    public override string ItemId      => "bean";
    public override string DisplayName => "Beans";
    public override string Description => "A handful of speckled beans shelled from their pods";
    public override int PriceReference => 3;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PulpHumor>(50).Add<FiberHumor>(30).Add<FatHumor>(20);
}

public sealed class Hops : ConsumableItem
{
    public override string ItemId      => "hops";
    public override string DisplayName => "Hops";
    public override string Description => "A string of papery green hop cones, sticky with yellow dust inside";
    public override List<ItemTag> Tags => new() { ItemTag.Crop };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    public override string Article => "some";
    public override int PriceReference => 4;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<EtherHumor>(40).Add<FumeHumor>(35).Add<ZenHumor>(25);
}

// ── roots and leaves ────────────────────────────────────────────────────────

public sealed class Potato : VegetableItem
{
    public override string ItemId      => "potato";
    public override string DisplayName => "Potato";
    public override string Description => "A knobbly brown potato, earth still in its eyes";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PulpHumor>(65).Add<AquaHumor>(20).Add<FiberHumor>(15);
}

public sealed class Endive : VegetableItem
{
    public override string ItemId      => "endive";
    public override string DisplayName => "Endive";
    public override string Description => "A pale, tight head of forced endive, grown blanched in the dark";
    public override int PriceReference => 6;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AquaHumor>(45).Add<FiberHumor>(30).Add<YellowBileHumor>(25);
}

public sealed class Tomato : VegetableItem
{
    public override string ItemId      => "tomato";
    public override string DisplayName => "Tomato";
    public override string Description => "A soft red tomato split a little at the shoulder";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AquaHumor>(50).Add<SugarHumor>(25).Add<PulpHumor>(25);
}

// ── tree fruit and nuts ─────────────────────────────────────────────────────

public sealed class Olive : FruitItem
{
    public override string ItemId      => "olive";
    public override string DisplayName => "Olives";
    public override string Description => "A handful of black olives, bitter until they have been cured";
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 4;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(55).Add<PulpHumor>(25).Add<SaltHumor>(20);
}

public sealed class Lemon : FruitItem
{
    public override string ItemId      => "lemon";
    public override string DisplayName => "Lemon";
    public override string Description => "A thick-skinned yellow lemon, the oil sharp on the fingers";
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 5;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<CholerHumor>(45).Add<AquaHumor>(35).Add<SugarHumor>(20);
}

public sealed class Orange : FruitItem
{
    public override string ItemId      => "orange";
    public override string DisplayName => "Orange";
    public override string Description => "A round orange, its skin pitted and fragrant";
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 5;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(45).Add<AquaHumor>(35).Add<LaetitiaHumor>(20);
}

public sealed class Mango : FruitItem
{
    public override string ItemId      => "mango";
    public override string DisplayName => "Mango";
    public override string Description => "A heavy mango blushed red at the shoulder, soft under the thumb";
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 5;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(55).Add<PulpHumor>(30).Add<VoluptasHumor>(15);
}

public sealed class Almond : FruitItem
{
    public override string ItemId      => "almond";
    public override string DisplayName => "Almonds";
    public override string Description => "A handful of almonds in their pitted shells";
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 6;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(55).Add<FiberHumor>(25).Add<CalxHumor>(20);
}

public sealed class Coconut : FruitItem
{
    public override string ItemId      => "coconut";
    public override string DisplayName => "Coconut";
    public override string Description => "A brown hairy coconut, its water sloshing when shaken";
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 6;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(45).Add<AquaHumor>(35).Add<FiberHumor>(20);
}

public sealed class Grape : FruitItem
{
    public override string ItemId      => "grape";
    public override string DisplayName => "Grapes";
    public override string Description => "A bunch of dusty dark grapes, some split and sweet with wasps";
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 4;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(55).Add<AquaHumor>(25).Add<LaetitiaHumor>(20);
}

// ── cash crops ──────────────────────────────────────────────────────────────

public sealed class CottonBoll : TextileItem
{
    public override string ItemId      => "cotton_boll";
    public override string DisplayName => "Cotton Boll";
    public override string Description => "A burst cotton boll, the white fibre packed round its seeds";
    public override int PriceReference => 5;
}

public sealed class Sugarcane : ConsumableItem
{
    public override string ItemId      => "sugarcane";
    public override string DisplayName => "Sugarcane";
    public override string Description => "A length of jointed sugarcane, sweet sap at the cut end";
    public override List<ItemTag> Tags => new() { ItemTag.Crop };
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 3;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(70).Add<FiberHumor>(20).Add<AquaHumor>(10);
}

public sealed class TeaLeaf : HerbItem
{
    public override string ItemId      => "tea_leaf";
    public override string DisplayName => "Tea Leaves";
    public override string Description => "A pinch of dried tea leaves rolled tight and dark";
    public override int PriceReference => 8;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<NervusHumor>(45).Add<EtherHumor>(30).Add<ZenHumor>(25);
}

public sealed class Cinnamon : HerbItem
{
    public override string ItemId      => "cinnamon";
    public override string DisplayName => "Cinnamon";
    public override string Description => "A curl of cinnamon bark, sweet and hot to smell";
    public override int PriceReference => 12;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<CholerHumor>(40).Add<EtherHumor>(35).Add<LaetitiaHumor>(25);
}

public sealed class Clove : HerbItem
{
    public override string ItemId      => "clove";
    public override string DisplayName => "Cloves";
    public override string Description => "A twist of paper holding dried cloves like small nails";
    public override int PriceReference => 12;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<CholerHumor>(40).Add<FumeHumor>(30).Add<EtherHumor>(30);
}

// ── what is made of them ────────────────────────────────────────────────────

public sealed class OliveOil : ConsumableItem
{
    public override string ItemId      => "olive_oil";
    public override string DisplayName => "Olive Oil";
    public override string Description => "A stoppered flask of green-gold oil from the press";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 10;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(75).Add<PulpHumor>(15).Add<ConstantiaHumor>(10);
}

public sealed class SugarLoaf : ConsumableItem
{
    public override string ItemId      => "sugar_loaf";
    public override string DisplayName => "Sugar Loaf";
    public override string Description => "A small cone of brown sugar wrapped in blue paper";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 14;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(80).Add<LaetitiaHumor>(20);
}

public sealed class Cider : ConsumableItem
{
    public override string ItemId      => "cider";
    public override string DisplayName => "Cider";
    public override string Description => "A jug of cloudy cider, sharp with apple and a little sour";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Drink;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 5;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AlcoholHumor>(45).Add<SugarHumor>(35).Add<AquaHumor>(20);
}

public sealed class Raisins : ConsumableItem
{
    public override string ItemId      => "raisins";
    public override string DisplayName => "Raisins";
    public override string Description => "A pouch of dried grapes, sticky and dark";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override string Article => "some";
    public override int PriceReference => 5;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(65).Add<FiberHumor>(20).Add<LaetitiaHumor>(15);
}

public sealed class Copra : ConsumableItem
{
    public override string ItemId      => "copra";
    public override string DisplayName => "Copra";
    public override string Description => "Strips of dried coconut flesh, white and oily";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override string Article => "some";
    public override int PriceReference => 4;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(65).Add<FiberHumor>(35);
}

public sealed class TeaBrick : ConsumableItem
{
    public override string ItemId      => "tea_brick";
    public override string DisplayName => "Tea Brick";
    public override string Description => "A pressed brick of dark tea stamped with a maker's seal";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Modest;
    public override bool IsHard => true;
    public override int PriceReference => 15;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<NervusHumor>(50).Add<ZenHumor>(30).Add<EtherHumor>(20);
}

public sealed class Cornmeal : ConsumableItem
{
    public override string ItemId      => "cornmeal";
    public override string DisplayName => "Cornmeal";
    public override string Description => "A cloth bag of coarse yellow meal ground from maize";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override string Article => "some";
    public override int PriceReference => 4;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PulpHumor>(60).Add<SugarHumor>(20).Add<FiberHumor>(20);
}

public sealed class Oatcake : ConsumableItem
{
    public override string ItemId      => "oatcake";
    public override string DisplayName => "Oatcake";
    public override string Description => "A flat round oatcake baked hard on a griddle";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override bool IsHard => true;
    public override int PriceReference => 2;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(50).Add<PulpHumor>(35).Add<FatHumor>(15);
}

public sealed class RiceWine : ConsumableItem
{
    public override string ItemId      => "rice_wine";
    public override string DisplayName => "Rice Wine";
    public override string Description => "A small stoneware jar of clear, faintly sweet rice wine";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Drink;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 9;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AlcoholHumor>(60).Add<SugarHumor>(20).Add<ZenHumor>(20);
}

public sealed class SpicedWine : ConsumableItem
{
    public override string ItemId      => "spiced_wine";
    public override string DisplayName => "Spiced Wine";
    public override string Description => "A flask of red wine steeped with cinnamon and cloves";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Drink;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 16;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AlcoholHumor>(45).Add<CholerHumor>(25).Add<LaetitiaHumor>(30);
}

public sealed class Perry : ConsumableItem
{
    public override string ItemId      => "perry";
    public override string DisplayName => "Perry";
    public override string Description => "A jug of pale perry pressed from hard pears";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Drink;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 5;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AlcoholHumor>(40).Add<SugarHumor>(40).Add<AquaHumor>(20);
}

public sealed class BrinedOlives : ConsumableItem
{
    public override string ItemId      => "brined_olives";
    public override string DisplayName => "Brined Olives";
    public override string Description => "A crock of olives cured in salt water and herbs";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 7;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(45).Add<SaltHumor>(40).Add<PulpHumor>(15);
}

public sealed class Tempeh : ConsumableItem
{
    public override string ItemId      => "tempeh";
    public override string DisplayName => "Bean Cake";
    public override string Description => "A pressed cake of fermented beans bound in a leaf";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 3;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PulpHumor>(45).Add<FungiHumor>(30).Add<FatHumor>(25);
}

// ── field and grove implements ──────────────────────────────────────────────

public sealed class PruningHook : Item
{
    public override string ItemId      => "pruning_hook";
    public override string DisplayName => "Pruning Hook";
    public override string Description => "A short curved blade on an ash handle, for taking dead wood out of a tree";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override int UsageLevel => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 12;
}

public sealed class GraftingKnife : Item
{
    public override string ItemId      => "grafting_knife";
    public override string DisplayName => "Grafting Knife";
    public override string Description => "A small folding knife with a thin, very sharp blade and a bone spatula at the heel";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override int UsageLevel => 5;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 14;
}

public sealed class VineKnife : Item
{
    public override string ItemId      => "vine_knife";
    public override string DisplayName => "Vine Knife";
    public override string Description => "A hooked knife with a hatchet back, for cutting grape bunches and old canes";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override int UsageLevel => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 10;
}

public sealed class Machete : Item
{
    public override string ItemId      => "machete";
    public override string DisplayName => "Machete";
    public override string Description => "A long broad cutting blade with a wooden grip, notched from cane";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override int UsageLevel => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 14;
}

public sealed class PickingBasket : Item
{
    public override string ItemId      => "picking_basket";
    public override string DisplayName => "Picking Basket";
    public override string Description => "A deep basket worn on the back, its straps dark with sweat";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Large;
    public override WeightClass Weight => WeightClass.Light;
    public override int UsageLevel => 2;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 4;
}

public sealed class WinnowingFan : Item
{
    public override string ItemId      => "winnowing_fan";
    public override string DisplayName => "Winnowing Fan";
    public override string Description => "A broad flat basket with a lip at the back, for tossing grain into the wind";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override int UsageLevel => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Craftware };
    public override int PriceReference => 4;
}

public sealed class Mattock : Item
{
    public override string ItemId      => "mattock";
    public override string DisplayName => "Mattock";
    public override string Description => "A heavy iron head, an adze one side and a pick the other, on a thick haft";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override int UsageLevel => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 18;
}

public sealed class WateringCan : Item
{
    public override string ItemId      => "watering_can";
    public override string DisplayName => "Watering Pot";
    public override string Description => "A copper pot with a pierced rose on its long spout";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override int UsageLevel => 2;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 10;
}
