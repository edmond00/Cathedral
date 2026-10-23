using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Trade category tags for items. An item may carry several tags; an NPC's
/// <c>BuyTag</c>/<c>SellTag</c> reference a single tag to decide what it will trade.
///
/// This is deliberately SEPARATE from <see cref="ItemType"/> (which mirrors equipment
/// anchors — Headgear, Footwear, …). Tags are a free economic taxonomy, in two layers:
/// <list type="bullet">
/// <item>the <b>broad</b> tags (Tool … Pelt), which every tradeable item carries one of, and which
///       general dealers and buyers read — an innkeeper sells <see cref="Foodstuff"/>, a farmer buys
///       <see cref="Tool"/>;</item>
/// <item>the <b>trade</b> tags (Bakery … Footwear), each the shelf of one craft, which an item carries
///       beside its broad tag. A catalogue is a draw of about five items from the whole tag, so a
///       craftsman given a broad tag sold whatever it held: a cooper offered reading lenses and
///       relics, a baker tripe and ale, a barber pitchforks. <c>--item-audit</c> prints who sells
///       from each tag and what is in it.</item>
/// </list>
/// </summary>
public enum ItemTag
{
    /// <summary>Forged working tools.</summary>
    Tool,
    /// <summary>The smith's wares: tools + weapons + iron fittings.</summary>
    Ironwork,
    /// <summary>Ore, ingots, coal, stone, clay.</summary>
    Mineral,
    /// <summary>Raw / cut timber.</summary>
    Wood,
    /// <summary>Crafted household &amp; trade gear (vessels, containers, light, fishing gear).</summary>
    Craftware,
    /// <summary>Raw fibre + cloth.</summary>
    Textile,
    /// <summary>Finished garments.</summary>
    Clothing,
    /// <summary>Plant produce: vegetables, fruit, grain, fodder.</summary>
    Crop,
    /// <summary>Prepared / animal foods: bread, ale, dairy, meat, salt.</summary>
    Foodstuff,
    /// <summary>The catch — fish and shellfish.</summary>
    Fish,
    /// <summary>Culinary &amp; medicinal herbs.</summary>
    Herb,
    /// <summary>Wild-gathered plants &amp; nuts.</summary>
    Forage,
    /// <summary>Hides, furs, feathers, trophies (wild drops; no core vendor yet — future trapper).</summary>
    Pelt,

    // ── Trade tags: one craft's shelf each ─────────────────────────────────
    /// <summary>The baker's: bread, cakes, pies, and the meal they are made of.</summary>
    Bakery,
    /// <summary>The brewer's: ale, beer, mead, cider, perry.</summary>
    Ale,
    /// <summary>The vintner's: wine, and the grapes and raisins it comes from.</summary>
    Wine,
    /// <summary>The butcher's, the drover's and the swineherd's: meat, offal, fat.</summary>
    Meat,
    /// <summary>The dairymaid's: milk, butter, cheese, curds.</summary>
    Dairy,
    /// <summary>The poultry keeper's: eggs, fowl, feathers.</summary>
    Poultry,
    /// <summary>The miller's and the reaper's: grain, and the flour ground from it.</summary>
    Grain,
    /// <summary>The hayward's, and what the groom buys: hay, straw, oats, mast.</summary>
    Fodder,
    /// <summary>The orchardist's and the picker's: tree fruit and nuts.</summary>
    Fruit,
    /// <summary>The planter's: cane, cacao, cotton, tea, coconut.</summary>
    CashCrop,
    /// <summary>The miner's: ore, coal, flint.</summary>
    Ore,
    /// <summary>The charcoal burner's: coal, peat, tar, lye, cordwood.</summary>
    Fuel,
    /// <summary>The woodcutter's: logs, beams, planks, bark — wood not yet made into anything.</summary>
    Timber,
    /// <summary>The carpenter's: wooden tools and household wares.</summary>
    Woodwork,
    /// <summary>The cooper's: barrels, kegs, tubs, buckets, pails.</summary>
    Cooperage,
    /// <summary>The chandler's: candles, wax, soap, lamps.</summary>
    Chandlery,
    /// <summary>The potter's: pots, jugs, bowls, jars.</summary>
    Pottery,
    /// <summary>The sacristan's: the vessels, books and tokens of worship.</summary>
    Devotional,
    /// <summary>The steward's: ledgers, letters, seals, writing gear.</summary>
    Writing,
    /// <summary>The barber-surgeon's: razors, lancets, bandages, soap.</summary>
    Surgery,
    /// <summary>The quartermaster's: weapons and armour.</summary>
    Arms,
    /// <summary>What the fisherman buys: hooks, lines, nets, rods, rope.</summary>
    FishingGear,
    /// <summary>The shepherd's: fleece, wool, and what is knitted of it.</summary>
    Woollens,
    /// <summary>The weaver's and the laundress's: woven cloth and thread.</summary>
    Cloth,
    /// <summary>The cobbler's: shoes and boots, and the nails and lasts that make them.</summary>
    Footwear,
}

public static class ItemTagExtensions
{
    /// <summary>
    /// Human-readable label for a tag, used in trade verb verbatims and the trade UI
    /// (e.g. "propose to buy <b>tools</b> from …"). Falls back to the enum name.
    /// </summary>
    public static string Label(this ItemTag tag) =>
        _labels.TryGetValue(tag, out var label) ? label : tag.ToString();

    private static readonly Dictionary<ItemTag, string> _labels = new()
    {
        [ItemTag.Tool]      = "tools",
        [ItemTag.Ironwork]  = "ironwork",
        [ItemTag.Mineral]   = "ore and coal",
        [ItemTag.Wood]      = "timber",
        [ItemTag.Craftware] = "crafted goods",
        [ItemTag.Textile]   = "cloth and fibre",
        [ItemTag.Clothing]  = "garments",
        [ItemTag.Crop]      = "produce",
        [ItemTag.Foodstuff] = "victuals",
        [ItemTag.Fish]      = "fish",
        [ItemTag.Herb]      = "herbs",
        [ItemTag.Forage]    = "foraged goods",
        [ItemTag.Pelt]      = "hides and pelts",
        [ItemTag.Bakery]      = "bread and pies",
        [ItemTag.Ale]         = "ale and cider",
        [ItemTag.Wine]        = "wine",
        [ItemTag.Meat]        = "meat",
        [ItemTag.Dairy]       = "milk and cheese",
        [ItemTag.Poultry]     = "eggs and fowl",
        [ItemTag.Grain]       = "grain and flour",
        [ItemTag.Fodder]      = "hay and fodder",
        [ItemTag.Fruit]       = "fruit",
        [ItemTag.CashCrop]    = "the plantation's crop",
        [ItemTag.Ore]         = "ore",
        [ItemTag.Fuel]        = "coal and fuel",
        [ItemTag.Timber]      = "timber",
        [ItemTag.Woodwork]    = "woodwork",
        [ItemTag.Cooperage]   = "casks and buckets",
        [ItemTag.Chandlery]   = "candles and soap",
        [ItemTag.Pottery]     = "pots and jugs",
        [ItemTag.Devotional]  = "holy things",
        [ItemTag.Writing]     = "writing goods",
        [ItemTag.Surgery]     = "razors and remedies",
        [ItemTag.Arms]        = "arms and armour",
        [ItemTag.FishingGear] = "fishing gear",
        [ItemTag.Woollens]    = "wool and woollens",
        [ItemTag.Cloth]       = "cloth",
        [ItemTag.Footwear]    = "shoes and boots",
    };
}
