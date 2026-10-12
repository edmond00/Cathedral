using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative.World.Items;

// ─────────────────────────────────────────────────────────────────────────────
//  The trades of a dense city: what the cobbler, tailor, chandler, butcher, tanner, potter, apothecary
//  and barber work with and sell, and what the porter, laundress, water-carrier and beggar carry on
//  the street. The new shops (CityTradeSubfactory) stock their shelves from this file, and the city
//  archetypes draw their loadouts on it.
// ─────────────────────────────────────────────────────────────────────────────

// ── cobbler ───────────────────────────────────────────────────────────────

public sealed class Awl : ToolItem
{
    public override string ItemId      => "awl";
    public override string DisplayName => "Awl";
    public override string Description => "A short steel spike set in a round wooden haft, for piercing leather";
    public override ItemSize Size => ItemSize.Small;
    public override int PriceReference => 6;
}

public sealed class ShoeLast : Item
{
    public override string ItemId      => "shoe_last";
    public override string DisplayName => "Shoe Last";
    public override string Description => "A carved wooden foot, dark with handling, that a shoe is shaped over";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Wood, ItemTag.Craftware };
    public override int PriceReference => 5;
}

public sealed class Hobnails : Item
{
    public override string ItemId      => "hobnails";
    public override string DisplayName => "Hobnails";
    public override string Article     => "some";
    public override string Description => "A twist of paper holding a handful of broad-headed nails for boot soles";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork };
    public override int PriceReference => 3;
}

public sealed class BespokeShoes : WearableItem
{
    public override string ItemId      => "bespoke_shoes";
    public override string DisplayName => "Bespoke Shoes";
    public override string Description => "Square-toed shoes of good black leather, made to one foot and polished to a shine";
    public override ItemSize Size => ItemSize.Medium;
    public override WearSlot Slot => WearSlot.Footwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Bourgeois, SocialCategory.Aristocrat };
    public override int PriceReference => 30;
}

// ── tailor ────────────────────────────────────────────────────────────────

public sealed class Thimble : Item
{
    public override string ItemId      => "thimble";
    public override string DisplayName => "Thimble";
    public override string Description => "A brass thimble pitted all over by the heads of needles";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 3;
}

public sealed class SewingNeedles : Item
{
    public override string ItemId      => "sewing_needles";
    public override string DisplayName => "Sewing Needles";
    public override string Article     => "some";
    public override string Description => "A felt book stuck with fine steel needles of several sizes";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork, ItemTag.Craftware };
    public override int PriceReference => 6;
}

public sealed class TailorsChalk : Item
{
    public override string ItemId      => "tailors_chalk";
    public override string DisplayName => "Tailor's Chalk";
    public override string Description => "A flat cake of white chalk worn to an edge for marking cloth";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Mineral };
    public override int PriceReference => 1;
}

public sealed class Waistcoat : WearableItem
{
    public override string ItemId      => "waistcoat";
    public override string DisplayName => "Waistcoat";
    public override string Description => "A close-cut sleeveless coat of brocade, buttoned to the throat";
    public override ItemSize Size => ItemSize.Medium;
    public override WearSlot Slot => WearSlot.Bodywear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Bourgeois };
    public override int PriceReference => 24;
}

// ── chandler ──────────────────────────────────────────────────────────────

public sealed class Wick : Item
{
    public override string ItemId      => "wick";
    public override string DisplayName => "Wick";
    public override string Description => "A hank of plaited cotton wick, stiff with old tallow";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Textile };
    public override int PriceReference => 1;
}

public sealed class Beeswax : Item
{
    public override string ItemId      => "beeswax";
    public override string DisplayName => "Beeswax";
    public override string Article     => "some";
    public override string Description => "A yellow cake of beeswax that smells faintly of honey";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 9;
}

public sealed class CandleMould : Item
{
    public override string ItemId      => "candle_mould";
    public override string DisplayName => "Candle Mould";
    public override string Description => "A frame of six tin tubes for casting candles, crusted with old wax";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork, ItemTag.Craftware };
    public override int PriceReference => 10;
}

// ── butcher ───────────────────────────────────────────────────────────────

public sealed class Cleaver : ToolItem
{
    public override string ItemId      => "cleaver";
    public override string DisplayName => "Cleaver";
    public override string Description => "A heavy square-bladed chopper, its edge nicked by bone";
    public override WeightClass Weight => WeightClass.Medium;
    public override int PriceReference => 14;
}

public sealed class MeatHook : Item
{
    public override string ItemId      => "meat_hook";
    public override string DisplayName => "Meat Hook";
    public override string Description => "A double-ended iron S-hook, the kind joints are hung from";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork };
    public override int PriceReference => 3;
}

public sealed class Tripe : ConsumableItem
{
    public override string ItemId      => "tripe";
    public override string DisplayName => "Tripe";
    public override string Article     => "some";
    public override string Description => "A pale, honeycombed strip of boiled stomach, cheap and filling";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Modest;
    public override int PriceReference => 2;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PhlegmHumor>(45).Add<FatHumor>(35).Add<BloodHumor>(20);
}

public sealed class BloodPudding : ConsumableItem
{
    public override string ItemId      => "blood_pudding";
    public override string DisplayName => "Blood Pudding";
    public override string Description => "A dark, heavy sausage of blood, oats and suet";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Rich;
    public override int PriceReference => 5;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(55).Add<FatHumor>(25).Add<PulpHumor>(20);
}

public sealed class LeatherApron : WearableItem
{
    public override string ItemId      => "leather_apron";
    public override string DisplayName => "Leather Apron";
    public override string Description => "A stiff bib apron of oiled cowhide, stained dark at the front";
    public override ItemSize Size => ItemSize.Medium;
    public override WearSlot Slot => WearSlot.Outerwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Bourgeois };
    public override int PriceReference => 8;
}

// ── tanner ────────────────────────────────────────────────────────────────

public sealed class FleshingKnife : ToolItem
{
    public override string ItemId      => "fleshing_knife";
    public override string DisplayName => "Fleshing Knife";
    public override string Description => "A long curved blade with a handle at each end, for scraping hides";
    public override int PriceReference => 12;
}

public sealed class TannedLeather : Item
{
    public override string ItemId      => "tanned_leather";
    public override string DisplayName => "Tanned Leather";
    public override string Description => "A folded side of brown bark-tanned leather, supple and smelling of oak";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Pelt, ItemTag.Textile };
    public override int PriceReference => 18;
}

public sealed class TanBark : Item
{
    public override string ItemId      => "tan_bark";
    public override string DisplayName => "Tan Bark";
    public override string Article     => "some";
    public override string Description => "A sack of ground oak bark, brown and bitter-smelling";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Wood };
    public override int PriceReference => 3;
}

// ── potter ────────────────────────────────────────────────────────────────

public sealed class EarthenJug : Item
{
    public override string ItemId      => "earthen_jug";
    public override string DisplayName => "Earthen Jug";
    public override string Description => "A round-bellied jug of glazed brown earthenware with a thumb-pressed handle";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 4;
}

public sealed class PottersRib : Item
{
    public override string ItemId      => "potters_rib";
    public override string DisplayName => "Potter's Rib";
    public override string Description => "A smooth curved slip of horn used to shape the wall of a pot on the wheel";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 2;
}

public sealed class GlazedBowl : Item
{
    public override string ItemId      => "glazed_bowl";
    public override string DisplayName => "Glazed Bowl";
    public override string Description => "A shallow bowl with a running green glaze inside and a bare foot";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 3;
}

// ── apothecary ────────────────────────────────────────────────────────────

public sealed class Theriac : ConsumableItem
{
    public override string ItemId      => "theriac";
    public override string DisplayName => "Theriac";
    public override string Article     => "some";
    public override string Description => "A little pot of black, bitter electuary sold as a cure for every poison";
    public override List<ItemTag> Tags => new() { ItemTag.Herb };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Modest;
    public override int PriceReference => 25;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<OpiumHumor>(40).Add<SugarHumor>(35).Add<BlackBileHumor>(25);
}

public sealed class Albarello : Item
{
    public override string ItemId      => "albarello";
    public override string DisplayName => "Albarello";
    public override string Description => "A waisted apothecary's jar of painted majolica, its label in a careful hand";
    public override ItemCategory Category => ItemCategory.Container;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 12;
}

public sealed class Pestle : Item
{
    public override string ItemId      => "pestle";
    public override string DisplayName => "Pestle";
    public override string Description => "A heavy bronze pestle, polished at the end by grinding";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 8;
}

// ── barber ────────────────────────────────────────────────────────────────

public sealed class Razor : ToolItem
{
    public override string ItemId      => "razor";
    public override string DisplayName => "Razor";
    public override string Description => "A folding steel razor with a horn handle, honed thin as a leaf";
    public override ItemSize Size => ItemSize.Small;
    public override int PriceReference => 12;
}

public sealed class Lancet : ToolItem
{
    public override string ItemId      => "lancet";
    public override string DisplayName => "Lancet";
    public override string Description => "A small double-edged blade in a tortoiseshell case, for letting blood";
    public override ItemSize Size => ItemSize.Small;
    public override int PriceReference => 10;
}

public sealed class BleedingBowl : Item
{
    public override string ItemId      => "bleeding_bowl";
    public override string DisplayName => "Bleeding Bowl";
    public override string Description => "A pewter bowl with a notch cut in the rim to fit against an arm";
    public override ItemCategory Category => ItemCategory.Container;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 6;
}

// ── the street ────────────────────────────────────────────────────────────

public sealed class Tumpline : Item
{
    public override string ItemId      => "tumpline";
    public override string DisplayName => "Tumpline";
    public override string Description => "A broad leather strap a porter passes over the brow to take a load on the back";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 4;
}

public sealed class Lye : Item
{
    public override string ItemId      => "lye";
    public override string DisplayName => "Lye";
    public override string Article     => "some";
    public override string Description => "A stoppered crock of grey wood-ash lye that bites the skin";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Mineral };
    public override int PriceReference => 2;
}

public sealed class WashBeetle : Item
{
    public override string ItemId      => "wash_beetle";
    public override string DisplayName => "Wash Beetle";
    public override string Description => "A flat wooden bat, worn pale, for beating wet linen on a stone";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Wood };
    public override int PriceReference => 2;
}

public sealed class WaterYoke : Item
{
    public override string ItemId      => "water_yoke";
    public override string DisplayName => "Water Yoke";
    public override string Description => "A shaped shoulder yoke with a chain and hook hanging at each end";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Large;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Wood };
    public override int PriceReference => 5;
}

public sealed class LeatherBucket : Item
{
    public override string ItemId      => "leather_bucket";
    public override string DisplayName => "Leather Bucket";
    public override string Description => "A tarred leather bucket with a rope handle, sweating a little at the seams";
    public override ItemCategory Category => ItemCategory.Container;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 6;
}

public sealed class BeggingBowl : Item
{
    public override string ItemId      => "begging_bowl";
    public override string DisplayName => "Begging Bowl";
    public override string Description => "A chipped wooden bowl with a single copper left in it for luck";
    public override ItemCategory Category => ItemCategory.Container;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Wood };
    public override int PriceReference => 1;
}

public sealed class Crutch : Item
{
    public override string ItemId      => "crutch";
    public override string DisplayName => "Crutch";
    public override string Description => "A forked stick padded with rags at the top, polished by an armpit";
    public override ItemCategory Category => ItemCategory.Other;
    public override ItemSize Size => ItemSize.Large;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Wood };
    public override int PriceReference => 1;
}

public sealed class RaggedCloak : WearableItem
{
    public override string ItemId      => "ragged_cloak";
    public override string DisplayName => "Ragged Cloak";
    public override string Description => "A cloak more patch than cloth, held shut with a thorn";
    public override ItemSize Size => ItemSize.Medium;
    public override WearSlot Slot => WearSlot.Outerwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Pauper };
    public override int PriceReference => 1;
}
