using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Trade category tags for items. An item may carry several tags; an NPC's
/// <c>BuyTag</c>/<c>SellTag</c> reference a single tag to decide what it will trade.
///
/// This is deliberately SEPARATE from <see cref="ItemType"/> (which mirrors equipment
/// anchors — Headgear, Footwear, …). Tags are a free economic taxonomy. The set is kept
/// lean: catalogues are broad on purpose (e.g. a dairymaid's <see cref="Foodstuff"/> stock
/// may also surface bread or eggs).
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
    };
}
