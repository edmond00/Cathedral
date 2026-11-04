using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Crafting;

namespace Cathedral.Game.Scene.Shared;

/// <summary>
/// Crafting stations laid over a finished scene, and beside every station the rack of one of its trades.
///
/// <para>The factories put the common stations where their trade lives: an anvil in every forge, a
/// workbench in every workshop. The rarer ones (the potter's wheel, the tallow vat, the leather bench)
/// stood only in a city trade's shop, and the four stations of the second generation of trades stood
/// nowhere. This pass spreads them without touching a factory:</para>
/// <list type="bullet">
///   <item>every workroom holding at most one station gains one more, drawn from <see cref="WorkroomPool"/>
///     — a house's workroom becomes a potter's, a chandler's, a carver's or a goldsmith's;</item>
///   <item>every scriptorium, chancery, counting room and library gains a writing desk;</item>
///   <item>a yard gains a vat, a wheel or a bench now and then (<see cref="YardChance"/>), from
///     <see cref="YardPool"/>: fat is rendered and pots thrown on farms as much as in towns;</item>
///   <item>a public square, green, market or lane gains a free station now and then
///     (<see cref="PublicChance"/>), from <see cref="PublicPool"/>: a mason's banker by the church, a
///     rendering vat behind the market. Nobody owns them, so no leave is asked;</item>
///   <item>beside every station, old or new, a rack of one of the trades worked there
///     (<see cref="AddRack"/>): it holds that trade's implement and teaches the trade on examination,
///     which is how a trade's will finds its tool and its lesson in the world.</item>
/// </list>
///
/// <para>Drawn from a generator of its own seeded with the location id, after everything else is built,
/// so nothing the factory or the people placed moves. Off in the test location, like the plays.</para>
/// </summary>
public static class StationPlacement
{
    public const double PublicChance = 0.3, YardChance = 0.25;

    private static Random StationRandom(int locationId) => new(unchecked(locationId * 0x5BD1E995 + 0x2F3A));

    private sealed record Kind(Type Station, string Name, string Description, string[] Moods, Func<string, List<string>, string[], PointOfInterest> Make);

    private static readonly Kind PottersWheel = new(typeof(PottersWheelPointOfInterest), "Potter's Wheel",
        "A kick wheel of heavy wood, the head crusted with dry slip", new[] { "clay-spattered", "heavy" },
        (n, d, m) => new PottersWheelPointOfInterest(n, d, null, m));
    private static readonly Kind TallowVat = new(typeof(TallowVatPointOfInterest), "Rendering Vat",
        "An iron vat on a brick hearth, a skin of grey fat set on the cold tallow", new[] { "greasy", "reeking" },
        (n, d, m) => new TallowVatPointOfInterest(n, d, null, m));
    private static readonly Kind LeatherBench = new(typeof(LeatherBenchPointOfInterest), "Leather Bench",
        "A low bench with a clamp at one end, its top scored by a thousand knife strokes", new[] { "scored", "waxy" },
        (n, d, m) => new LeatherBenchPointOfInterest(n, d, null, m));
    private static readonly Kind CarvingBench = new(typeof(CarvingBenchPointOfInterest), "Carving Bench",
        "A bench with a bone-dust tray under a vice, shavings of horn curled in the corners", new[] { "dusty", "fine" },
        (n, d, m) => new CarvingBenchPointOfInterest(n, d, null, m));
    private static readonly Kind MasonsBanker = new(typeof(MasonsBankerPointOfInterest), "Mason's Banker",
        "A waist-high stone bench on a bed of chippings, a half-dressed block still on it", new[] { "chipped", "dusty" },
        (n, d, m) => new MasonsBankerPointOfInterest(n, d, null, m));
    private static readonly Kind GoldsmithsBench = new(typeof(GoldsmithsBenchPointOfInterest), "Goldsmith's Bench",
        "A bench with a curved bite cut from its edge and a leather skin slung under it to catch the filings", new[] { "fine", "orderly" },
        (n, d, m) => new GoldsmithsBenchPointOfInterest(n, d, null, m));
    private static readonly Kind CuttingTable = new(typeof(CuttingTablePointOfInterest), "Cutting Table",
        "A long table under the window, chalk lines and pin holes all over its top", new[] { "long", "chalked" },
        (n, d, m) => new CuttingTablePointOfInterest(n, d, null, m));
    private static readonly Kind Loom = new(typeof(LoomPointOfInterest), "Loom",
        "A narrow upright loom against the wall, a half-woven length on it", new[] { "creaking", "half-woven" },
        (n, d, m) => new LoomPointOfInterest(n, d, null, m));
    private static readonly Kind WritingDesk = new(typeof(WritingDeskPointOfInterest), "Writing Desk",
        "A sloped desk with an ink horn sunk in its rail and a knife for the quills", new[] { "ink-stained", "quiet" },
        (n, d, m) => new WritingDeskPointOfInterest(n, d, null, m));

    /// <summary>What a workroom with room for it takes up.</summary>
    private static readonly Kind[] WorkroomPool = { PottersWheel, PottersWheel, TallowVat, TallowVat, LeatherBench, CarvingBench, MasonsBanker, GoldsmithsBench, GoldsmithsBench, GoldsmithsBench, CuttingTable, Loom };

    /// <summary>What a farmyard or a workyard keeps in a corner.</summary>
    private static readonly Kind[] YardPool = { TallowVat, PottersWheel, MasonsBanker, CarvingBench };

    /// <summary>What stands free in a public place.</summary>
    private static readonly Kind[] PublicPool = { MasonsBanker, PottersWheel, TallowVat, CarvingBench, LeatherBench };

    public static void Place(Scene scene, int locationId)
    {
        var rng = StationRandom(locationId);
        foreach (var area in scene.AllAreas)
        {
            int stations = area.PointsOfInterest.Count(CraftingStations.IsStation);
            switch (area)
            {
                case WorkshopArea when stations <= 1:
                    Add(area, WorkroomPool[rng.Next(WorkroomPool.Length)]);
                    break;
                case ScriptoriumArea or ChanceryArea or CountinghouseArea or LibraryArea when stations == 0:
                    Add(area, WritingDesk);
                    break;
                case YardArea when stations == 0:
                    if (rng.NextDouble() < YardChance) Add(area, YardPool[rng.Next(YardPool.Length)]);
                    break;
                case SquareArea or GreenArea or MarketArea or LaneArea when !area.IsPrivate && stations == 0:
                    if (rng.NextDouble() < PublicChance) Add(area, PublicPool[rng.Next(PublicPool.Length)]);
                    break;
            }
        }

        foreach (var area in scene.AllAreas)
            foreach (var station in area.PointsOfInterest.Where(CraftingStations.IsStation).ToList())
                AddRack(area, station, rng);
    }

    private static void Add(Area area, Kind kind)
    {
        if (area.PointsOfInterest.Any(p => p.GetType() == kind.Station)) return;
        area.PointsOfInterest.Add(kind.Make(kind.Name, new List<string> { kind.Description }, kind.Moods));
    }

    /// <summary>
    /// A rack beside <paramref name="station"/> for one trade of its kind, drawn: the trade's implement on
    /// it, and the trade taught by examining it. Named after the practitioner, so two racks in one area
    /// stay two.
    /// </summary>
    private static void AddRack(Area area, PointOfInterest station, Random rng)
    {
        var trades = CraftingModusMentis.All.Where(t => t.Station == station.GetType()).ToList();
        if (trades.Count == 0) return;
        var trade = trades[rng.Next(trades.Count)];
        var who   = trade.PersonaReminder ?? trade.DisplayName.ToLowerInvariant();
        var name  = char.ToUpperInvariant(who[0]) + who[1..] + "'s Rack";
        if (area.PointsOfInterest.Any(p => p.DisplayName == name)) return;
        var items = new List<ItemElement>();
        if (Activator.CreateInstance(trade.Tool) is Item tool) items.Add(new ItemElement(tool));
        area.PointsOfInterest.Add(new RackPointOfInterest(name,
            new List<string> { $"A rack by the {CraftingStations.Noun(station.GetType())} where a {who} keeps the tools of the trade, a pattern or two pinned above it" },
            items, new[] { "orderly", "worn" })
        {
            Senses = SensoryProfile.Examinable,
            VerbModiMentis = new Dictionary<string, string> { ["examine"] = trade.ModusMentisId },
        });
    }
}
