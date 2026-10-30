using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Shared;

/// <summary>
/// The quarters a town is made of: a market quarter, the tanners' streets, the streets round the
/// temple, the poor quarter by the wall. A city draws two or three of them and spreads them over its
/// ways; the town round a great building has one.
///
/// <para>A quarter decides what most of a town is made of, which is what made two cities the same city:
/// the bulk of any town is its houses and its streets, and they were one house and one street repeated.
/// Each quarter furnishes its streets (tanning pits, tenter frames, street shrines, a pillory), houses
/// its people in its own kinds of dwelling (tenements in the poor quarter, town houses where the
/// merchants live, workshop-houses where the trades are) and draws its own trades to it.</para>
/// </summary>
public static class TownQuarters
{
    public sealed record Quarter(
        string Name,
        Func<PointOfInterest>[] Furniture,
        (DwellingStyle Style, double Weight)[] Dwellings,
        string[] Trades);

    private static List<ItemElement> I(params Func<Item>[] items) => items.Select(f => new ItemElement(f())).ToList();
    private static Dictionary<string, string> T(params (string Verb, string Mm)[] t) => t.ToDictionary(x => x.Verb, x => x.Mm);

    public static readonly Quarter[] All =
    {
        new("Market Quarter",
            new Func<PointOfInterest>[]
            {
                () => new StallPointOfInterest("Market Stalls", new() { "A row of trestle stalls under patched awnings, cabbages, pots and ribbon side by side" }, I(() => new Cabbage(), () => new Narrative.World.Items.Thread()), new[] { "crowded", "patched" })
                      { Senses = SensoryProfile.FullyAlive, VerbModiMentis = T(("examine", "appraisal"), ("listen", "hawking"), ("smell", "relish"), ("contemplate", "worldliness")) },
                () => new AwningPointOfInterest("Striped Awning", new() { "A striped awning over a shopfront, the goods heaped under it out of the sun" }, I(() => new Linen()), new[] { "striped", "sagging" })
                      { Senses = SensoryProfile.Examinable, VerbModiMentis = T(("examine", "bargaining")) },
            },
            new[] { (DwellingStyle.WorkshopHouse, 2.0), (DwellingStyle.TownHouse, 1.0), (DwellingStyle.Plain, 1.0) },
            new[] { "Bakery", "Butcher's Shop", "Chandlery", "Cobbler's Shop" }),
        new("Tanners' Quarter",
            new Func<PointOfInterest>[]
            {
                () => new VatPointOfInterest("Tanning Pits", new() { "Pits sunk in the ground and full of reeking brown liquor, hides soaking in them" }, I(() => new Hide()), new[] { "reeking", "brown" })
                      { Senses = SensoryProfile.Odorous, VerbModiMentis = T(("examine", "tanning"), ("smell", "taint_sense")) },
                () => new RackPointOfInterest("Drying Hides", new() { "Hides stretched on wooden frames to dry, stinking in the sun" }, I(() => new Hide()), new[] { "stretched", "stinking" })
                      { Senses = SensoryProfile.Odorous, VerbModiMentis = T(("examine", "tanning"), ("smell", "carrion_sense")) },
            },
            new[] { (DwellingStyle.Tenement, 1.5), (DwellingStyle.WorkshopHouse, 1.5), (DwellingStyle.Plain, 1.0) },
            new[] { "Tannery", "Cobbler's Shop", "Butcher's Shop" }),
        new("Temple Quarter",
            new Func<PointOfInterest>[]
            {
                () => new ShrinePointOfInterest("Street Shrine", new() { "A shrine in a niche of the wall, a painted saint behind a grille, candle-ends below" }, I(() => new VotiveCandle()), new[] { "niched", "candle-lit" })
                      { Senses = SensoryProfile.Fragrant, VerbModiMentis = T(("examine", "iconography"), ("smell", "incensing"), ("contemplate", "devotion")) },
                () => new StatuePointOfInterest("Saint's Statue", new() { "A stone saint on a column at the street corner, worn smooth where people touch its foot" }, null, new[] { "worn", "touched" })
                      { Senses = SensoryProfile.Beautiful, VerbModiMentis = T(("examine", "hagiography"), ("contemplate", "piety")) },
            },
            new[] { (DwellingStyle.TownHouse, 1.5), (DwellingStyle.Plain, 1.5), (DwellingStyle.Cottage, 0.5) },
            new[] { "Chandlery", "Apothecary's Shop", "Bakery" }),
        new("Wall Quarter",
            new Func<PointOfInterest>[]
            {
                () => new BattlementPointOfInterest("Wall Steps", new() { "Stone steps going up the inside of the town wall to the walk, a watchman's brazier at the top" }, null, new[] { "steep", "watched" })
                      { Senses = SensoryProfile.Examinable, VerbModiMentis = T(("examine", "fortification")) },
                () => new PilloryPointOfInterest("Pillory", new() { "A pillory on a little platform by the wall, rotten vegetables trodden round it" }, null, new[] { "shameful", "trodden" })
                      { Senses = SensoryProfile.Beautiful, VerbModiMentis = T(("examine", "jurisprudence"), ("contemplate", "severity")) },
            },
            new[] { (DwellingStyle.Tenement, 1.0), (DwellingStyle.Cottage, 1.5), (DwellingStyle.ByreHouse, 1.0) },
            new[] { "Forge", "Mill", "Carpenter's Workshop" }),
        new("Weavers' Quarter",
            new Func<PointOfInterest>[]
            {
                () => new FramePointOfInterest("Tenter Frames", new() { "Long frames in an open yard, new cloth stretched on hooks to dry square" }, I(() => new Linen()), new[] { "long", "stretched" })
                      { Senses = SensoryProfile.Examinable, VerbModiMentis = T(("examine", "weave_reading")) },
                () => new RackPointOfInterest("Dyed Cloth", new() { "Lengths of cloth hung dripping from poles, madder red and woad blue" }, I(() => new Narrative.World.Items.Thread()), new[] { "dripping", "bright" })
                      { Senses = SensoryProfile.Beautiful, VerbModiMentis = T(("examine", "threadwork"), ("contemplate", "aesthetic")) },
            },
            new[] { (DwellingStyle.WorkshopHouse, 2.0), (DwellingStyle.Tenement, 1.0), (DwellingStyle.Plain, 1.0) },
            new[] { "Weaver's Workshop", "Tailor's Shop", "Pottery" }),
        new("Smiths' Quarter",
            new Func<PointOfInterest>[]
            {
                () => new AnvilPointOfInterest("Street Anvil", new() { "An anvil on a stump outside a smithy door, for work too big to take inside" }, I(() => new Coal()), new[] { "scarred", "ringing" })
                      { Senses = SensoryProfile.Audible, VerbModiMentis = T(("examine", "metalcraft"), ("listen", "forge_ear")) },
                () => new CoalPointOfInterest("Coal Heap", new() { "A heap of charcoal tipped against a wall, black dust over everything near it" }, I(() => new Coal()), new[] { "black", "dusty" })
                      { Senses = SensoryProfile.Examinable, VerbModiMentis = T(("examine", "firecraft")) },
            },
            new[] { (DwellingStyle.WorkshopHouse, 2.0), (DwellingStyle.Plain, 1.0), (DwellingStyle.Tenement, 0.5) },
            new[] { "Forge", "Cooper's Workshop", "Carpenter's Workshop" }),
        new("Poor Quarter",
            new Func<PointOfInterest>[]
            {
                () => new MiddenPointOfInterest("Refuse Heap", new() { "A heap of refuse at the end of the lane, picked over by dogs and children" }, I(() => new Straw(), () => new Bone()), new[] { "picked-over", "reeking" })
                      { Senses = SensoryProfile.Odorous, VerbModiMentis = T(("examine", "beggary"), ("smell", "taint_sense")) },
                () => new BarrelPointOfInterest("Rain Barrel", new() { "A barrel catching the drip from the eaves, the one water the lane has" }, I(() => new WaterDraught()), new[] { "green-slimed", "shared" })
                      { Senses = SensoryProfile.Examinable, VerbModiMentis = T(("examine", "streetwise")) },
            },
            new[] { (DwellingStyle.Tenement, 3.0), (DwellingStyle.Cottage, 1.0) },
            new[] { "Tannery", "Pottery", "Barber's Shop" }),
        new("Merchants' Quarter",
            new Func<PointOfInterest>[]
            {
                () => new FountainPointOfInterest("Merchants' Fountain", new() { "A fountain of carved stone paid for by the guilds, their arms cut round its basin" }, I(() => new WaterDraught()), new[] { "carved", "splashing" })
                      { Play = new PaddlePlay(), Senses = SensoryProfile.Audible, VerbModiMentis = T(("examine", "heraldry"), ("listen", "crowd_murmur")) },
                () => new CratePointOfInterest("Bonded Crates", new() { "Crates stacked outside a warehouse door, marked and sealed for the customs" }, I(() => new Rope()), new[] { "sealed", "stacked" })
                      { Senses = SensoryProfile.Examinable, VerbModiMentis = T(("examine", "brokerage")) },
            },
            new[] { (DwellingStyle.TownHouse, 3.0), (DwellingStyle.Plain, 1.0) },
            new[] { "Apothecary's Shop", "Tailor's Shop", "Chandlery" }),
    };

    /// <summary>Draws <paramref name="count"/> distinct quarters.</summary>
    public static List<Quarter> Draw(Random rng, int count)
        => All.OrderBy(_ => rng.Next()).Take(count).ToList();

    /// <summary>A dwelling style for a house in <paramref name="q"/>, by its weights.</summary>
    public static DwellingStyle Dwelling(Quarter q, Random rng)
    {
        double roll = rng.NextDouble() * q.Dwellings.Sum(d => d.Weight);
        foreach (var (style, w) in q.Dwellings)
        {
            if (roll < w) return style;
            roll -= w;
        }
        return q.Dwellings[^1].Style;
    }

    /// <summary>A dwelling style for a village house, where there are no quarters.</summary>
    public static DwellingStyle VillageDwelling(Random rng) => rng.NextDouble() switch
    {
        < 0.35 => DwellingStyle.Cottage,
        < 0.60 => DwellingStyle.ByreHouse,
        < 0.80 => DwellingStyle.WorkshopHouse,
        _      => DwellingStyle.Plain,
    };

    /// <summary>Furnishes each way with one piece of its quarter's street furniture.</summary>
    public static void Furnish(IReadOnlyDictionary<Area, Quarter> quarterOf, Random rng)
    {
        foreach (var (way, q) in quarterOf)
            way.PointsOfInterest.Add(q.Furniture[rng.Next(q.Furniture.Length)]());
    }
}
