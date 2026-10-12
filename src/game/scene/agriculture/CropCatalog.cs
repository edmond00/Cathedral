using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Glyph.Microworld;

namespace Cathedral.Game.Scene.Agriculture;

/// <summary>Builds the PoI a crop stands in its rows as: a kind, named and described by the caller.</summary>
public delegate PointOfInterest PlantKind(string displayName, List<string> descriptions, List<ItemElement>? items, string[]? moods,
                                          SensoryProfile senses, IReadOnlyDictionary<string, string> lessons);

/// <summary>
/// One crop, as an agriculture location grows it: what stands in the rows, what it yields, and how a
/// row of it is spoken of.
/// </summary>
/// <param name="Word">The crop as the location table names it: "radish" in "radish field".</param>
/// <param name="Title">Its title-case name, for areas: "Radish".</param>
/// <param name="Plant">What stands in a row of it, for the PoI: "Radish Row", "Pear Tree".</param>
/// <param name="PlantText">How that PoI is described.</param>
/// <param name="RowText">How the worked ground under it is described.</param>
/// <param name="Kind">The PoI kind the plant is, so lessons can gate on it by type.</param>
/// <param name="Yield">What a row gives up when it is gathered.</param>
/// <param name="Byproduct">What else comes off it — straw, leaves — or null.</param>
/// <param name="Made">What its people make of it, kept in the press-house or the store, or null.</param>
/// <param name="Lesson">What examining the plant teaches.</param>
public sealed record Crop(
    string Word, string Title, string Plant, string PlantText, string RowText,
    PlantKind Kind, Func<Item> Yield, Func<Item>? Byproduct, Func<Item>? Made, string Lesson);

/// <summary>
/// Every crop the location table grows. <b>Keyed by the table's own word</b> — which is the one place a
/// string names content here, and it is checked: <see cref="Validate"/> resolves every agriculture
/// location the table can put on the map and throws naming the first it cannot, and
/// <c>--building-audit</c> builds them all.
/// </summary>
public static class CropCatalog
{
    private static readonly PlantKind Row      = (n, d, i, m, s, l) => new CropPointOfInterest(n, d, i, m) { Senses = s, VerbModiMentis = l };
    private static readonly PlantKind Tree     = (n, d, i, m, s, l) => new TreePointOfInterest(n, d, i, m) { Senses = s, VerbModiMentis = l };
    private static readonly PlantKind VineKind = (n, d, i, m, s, l) => new VinePointOfInterest(n, d, i, m) { Senses = s, VerbModiMentis = l };
    private static readonly PlantKind Hop      = (n, d, i, m, s, l) => new HopbinePointOfInterest(n, d, i, m) { Senses = s, VerbModiMentis = l };
    private static readonly PlantKind Trellis  = (n, d, i, m, s, l) => new TrellisPointOfInterest(n, d, i, m) { Senses = s, VerbModiMentis = l };
    private static readonly PlantKind Cane     = (n, d, i, m, s, l) => new CanePointOfInterest(n, d, i, m) { Senses = s, VerbModiMentis = l };
    private static readonly PlantKind Palm     = (n, d, i, m, s, l) => new PalmPointOfInterest(n, d, i, m) { Senses = s, VerbModiMentis = l };
    private static readonly PlantKind TeaBush  = (n, d, i, m, s, l) => new TeaPointOfInterest(n, d, i, m) { Senses = s, VerbModiMentis = l };
    private static readonly PlantKind Cotton   = (n, d, i, m, s, l) => new CottonbollPointOfInterest(n, d, i, m) { Senses = s, VerbModiMentis = l };
    private static readonly PlantKind Bed      = (n, d, i, m, s, l) => new MushroombedPointOfInterest(n, d, i, m) { Senses = s, VerbModiMentis = l };
    private static readonly PlantKind Seedling = (n, d, i, m, s, l) => new SeedlingPointOfInterest(n, d, i, m) { Senses = s, VerbModiMentis = l };
    private static readonly PlantKind Furrow   = (n, d, i, m, s, l) => new FurrowPointOfInterest(n, d, i, m) { Senses = s, VerbModiMentis = l };
    private static readonly PlantKind HayKind  = (n, d, i, m, s, l) => new HayPointOfInterest(n, d, i, m) { Senses = s, VerbModiMentis = l };

    private static readonly Dictionary<string, Crop> _byWord = new Crop[]
    {
        // ── grain, pulse and fodder ────────────────────────────────────────────
        new("wheat",  "Wheat",  "Wheat Row",  "A row of wheat, the ears heavy and gold-brown and whispering", "long strips of standing wheat running off into the haze",
            Row, () => new Grain(), () => new Straw(), () => new Flour(), "harvestry"),
        new("barley", "Barley", "Barley Row", "A row of bearded barley, the awns catching the light", "strips of barley nodding under their long beards",
            Row, () => new Barley(), () => new Straw(), () => new Ale(), "harvestry"),
        new("rye",    "Rye",    "Rye Row",    "A row of tall grey-green rye, taller than a man's waist", "strips of rye standing tall and thin",
            Row, () => new Rye(), () => new Straw(), () => new Bread(), "harvestry"),
        new("oat",    "Oat",    "Oat Row",    "A row of oats, the loose heads hanging like little bells", "strips of pale oats rattling in the wind",
            Row, () => new Oats(), () => new Straw(), () => new Oatcake(), "harvestry"),
        new("corn",   "Maize",  "Maize Row",  "A row of tall maize, the ears sheathed and silk-topped", "tall stands of maize rustling over the heads of anyone between them",
            Row, () => new Maize(), null, () => new Cornmeal(), "harvestry"),
        new("hay",    "Hay",    "Hay Windrow","A long windrow of mown grass drying in the sun", "a meadow mown in swathes, the grass lying in long windrows",
            HayKind, () => new Hay(), () => new Hay(), null, "harvestry"),
        new("pea",    "Pea",    "Pea Row",    "A row of pea-vines climbing their sticks, pods hanging plump", "rows of peas climbing hazel sticks",
            Trellis, () => new Pea(), null, () => new DriedPeas(), "seed_lore"),
        new("bean",   "Bean",   "Bean Row",   "A row of bean-poles hung with long pods", "rows of beans on tall poles, the flowers red and white",
            Trellis, () => new Bean(), null, () => new Tempeh(), "seed_lore"),
        new("hop",    "Hop",    "Hop Bine",   "A tall pole wound with hop bines, the cones hanging papery and green", "rows of hop-poles strung with wire, the bines twisting up them",
            Hop, () => new Hops(), null, () => new Beer(), "hop_lore"),
        new("cotton", "Cotton", "Cotton Row", "A row of cotton bushes, the bolls burst white", "rows of low cotton bushes dotted white with open bolls",
            Cotton, () => new CottonBoll(), null, () => new CottonCloth(), "plantership"),

        // ── roots and leaves ─────────────────────────────────────────────────
        new("cabbage", "Cabbage", "Cabbage Row", "A row of cabbages, the leaves curling tight round their hearts", "rows of cabbages, blue-green and dewy",
            Furrow, () => new Cabbage(), null, null, "tillage"),
        new("carrot",  "Carrot",  "Carrot Row",  "A row of carrots, their feathery tops waving", "rows of feathery carrot tops in fine dark earth",
            Furrow, () => new Carrot(), null, null, "seed_lore"),
        new("onion",   "Onion",   "Onion Row",   "A row of onions, the tops gone yellow and bent", "rows of onions lifting their papery shoulders out of the soil",
            Furrow, () => new Onion(), null, null, "seed_lore"),
        new("turnip",  "Turnip",  "Turnip Row",  "A row of turnips, white shoulders pushing through the soil", "rows of turnips in heavy earth",
            Furrow, () => new Turnip(), null, null, "seed_lore"),
        new("radish",  "Radish",  "Radish Row",  "A row of radishes, red shoulders showing", "rows of radishes in crumbled soil",
            Furrow, () => new Radish(), null, null, "seed_lore"),
        new("potato",  "Potato",  "Potato Row",  "A row of earthed-up potatoes, the haulms dark and flowering", "long ridges of potatoes earthed up against the frost",
            Furrow, () => new Potato(), null, null, "tillage"),
        new("tomato",  "Tomato",  "Tomato Row",  "A row of tomato plants tied to canes, the fruit reddening", "rows of tomato plants staked and tied, smelling sharply green",
            Trellis, () => new Tomato(), null, null, "seed_lore"),

        // ── the dark crops of the cellars ────────────────────────────────────
        new("mushroom", "Mushroom", "Mushroom Bed", "A long bed of dark compost thick with pale mushrooms", "beds of rotted straw and dung where the spawn runs white",
            Bed, () => new Mushroom(), null, null, "blanching"),
        new("endive",   "Endive",   "Endive Bed",   "A bed of sand with pale forced endives packed in it like teeth", "beds of sand where roots are forced in the dark",
            Bed, () => new Endive(), null, null, "blanching"),

        // ── trees ────────────────────────────────────────────────────────────
        new("apple",   "Apple",   "Apple Tree",   "An apple tree, its branches propped against the weight of fruit", "rows of apple trees in long grass",
            Tree, () => new Apple(), null, () => new Cider(), "pomology"),
        new("pear",    "Pear",    "Pear Tree",    "A tall pear tree, the fruit speckled and hanging low", "rows of tall pear trees",
            Tree, () => new Pear(), null, () => new Perry(), "pomology"),
        new("cherry",  "Cherry",  "Cherry Tree",  "A cherry tree hung with dark fruit, birds loud in it", "rows of cherry trees, the bark glossy and banded",
            Tree, () => new Cherry(), null, null, "pomology"),
        new("mango",   "Mango",   "Mango Tree",   "A broad mango tree, fruit hanging on long stalks", "great shady mango trees in rows",
            Tree, () => new Mango(), null, null, "pomology"),
        new("olive",   "Olive",   "Olive Tree",   "An old olive tree, its trunk twisted like rope, the leaves silver", "rows of silver-leaved olive trees on dry ground",
            Tree, () => new Olive(), null, () => new OliveOil(), "pomology"),
        new("citrus",  "Lemon",   "Lemon Tree",   "A lemon tree, glossy-leaved and sharp-scented, fruit and flower together", "rows of glossy lemon trees",
            Tree, () => new Lemon(), null, null, "pomology"),
        new("orange",  "Orange",  "Orange Tree",  "An orange tree heavy with fruit, the blossom sweet", "rows of orange trees, the air sweet with blossom",
            Tree, () => new Orange(), null, null, "pomology"),
        new("almond",  "Almond",  "Almond Tree",  "An almond tree, the nuts in their furred green husks", "rows of almond trees on stony ground",
            Tree, () => new Almond(), null, null, "pomology"),
        new("coconut", "Coconut", "Coconut Palm", "A leaning coconut palm, the nuts clustered under its crown", "ranks of tall coconut palms",
            Palm, () => new Coconut(), () => new PalmFrond(), () => new Copra(), "plantership"),
        new("banana",  "Banana",  "Banana Plant", "A banana plant, broad leaves torn by the wind, a heavy hand of fruit hanging", "ranks of banana plants with leaves like ragged flags",
            Palm, () => new Banana(), () => new PalmFrond(), null, "plantership"),
        new("spice",   "Spice",   "Cinnamon Tree","A coppiced cinnamon tree, the bark stripped in curls from its shoots", "rows of coppiced spice trees and drying racks",
            Tree, () => new Cinnamon(), () => new Clove(), null, "plantership"),

        // ── the hot country's cash crops ─────────────────────────────────────
        new("sugarcane", "Sugarcane", "Cane Stand", "A stand of sugarcane three times a man's height", "rows of sugarcane standing like a wall",
            Cane, () => new Sugarcane(), null, () => new SugarLoaf(), "canecraft"),
        new("tea",       "Tea",       "Tea Bush",   "A tea bush clipped flat at waist height, new leaves bright at the top", "terraces of tea bushes clipped flat like hedges",
            TeaBush, () => new TeaLeaf(), null, () => new TeaBrick(), "tea_lore"),
        new("rice",      "Rice",      "Rice Shoots","A row of rice standing in water, heads beginning to droop", "flooded paddies of rice in still water",
            Seedling, () => new Rice(), () => new Straw(), () => new RiceWine(), "paddycraft"),
        new("vine",      "Vine",      "Vine Row",   "A row of vines tied along a wire, the grapes dusty and dark", "rows of vines on their stakes",
            VineKind, () => new Grape(), null, () => new Wine(), "viniculture"),
    }.ToDictionary(c => c.Word);

    /// <summary>The crop an agriculture location grows: "radish field" grows radish.</summary>
    public static Crop Of(string agricultureKey)
    {
        string word = SettlementTable.CropOf(agricultureKey);
        return _byWord.TryGetValue(word, out var crop)
            ? crop
            : throw new InvalidOperationException($"CropCatalog: no crop '{word}' for the location '{agricultureKey}'");
    }

    /// <summary>Throws naming the first agriculture location in the table whose crop is not catalogued.</summary>
    public static void Validate()
    {
        foreach (var key in SettlementTable.AllAgriculture) Of(key);
    }
}
