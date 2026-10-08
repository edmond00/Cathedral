using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Building;
using Cathedral.Glyph.Microworld;

namespace Cathedral.Game.Scene.Settled;

/// <summary>
/// A city: the town that sprawls out from an urban place of history — a citadel, a port, a palace or
/// an imperial school — across one to three cells of city ground. One layout generator
/// (<see cref="Shared.CityLayout.BuildDense"/>) for every city; the climate names and dresses its
/// streets and builds its houses (<see cref="SettledSceneFactory.Biome"/>).
///
/// <para><b>Dense on purpose.</b> A city cell is about twice the ways of the small town round a great
/// building, and <b>every way is lined with four or five doors</b> — no square, street, alley or gate
/// without a building on it. The doors are dealt out as slots before anything is built: each way
/// rolls its quota, the public buildings take the slots that suit them (the inns on a square, the
/// watch house at the gate, the doss house in an alley), and the townsfolk's houses fill every slot
/// that is left. The town round a great building is left at its old size
/// (<see cref="SettledSceneFactory.BuildTown"/>), so the great building stays what a visitor came
/// for.</para>
///
/// <para><b>People in proportion.</b> Every building houses somebody: the inns their keepers, the
/// merchant houses a merchant and a clerk, each of the city's trades a master in the shop and an
/// apprentice in a house of their own, the wash house its laundresses, the doss house its beggars,
/// and the houses one or two porters, water-carriers or clerks.</para>
///
/// <para><b>No harbour.</b> City ground is never shore, so a city cell has no waterfront, no harbour
/// inn and no sailors, even beside a port: those belong to the port's own cell
/// (<see cref="HistoricSceneFactory"/>), which is the only place in the country with ships.</para>
///
/// <para>Its size is rolled from its own seed, so neighbouring city cells read as different quarters of
/// one town rather than the same street repeated. Its name, when a world is behind it, is taken from
/// the place it grew around.</para>
/// </summary>
public sealed class CitySceneFactory : SettledSceneFactory
{
    public CitySceneFactory(string? biome = null, string? sessionPath = null) : base(biome, sessionPath) { }

    protected override string DefaultBiome => "plain";

    /// <summary>The fewest doors on any outdoor way of a city cell.</summary>
    public const int MinDoorsPerWay = 4;

    /// <summary>The chance a way takes a fifth door.</summary>
    private const double FifthDoorChance = 0.4;

    private Shared.CityPlan _plan = null!;

    protected override void BuildPlace(Random rng, int locationId, Scene scene)
    {
        int size = rng.Next(1, 4);
        string name = Site?.Origin is { } origin ? $"Streets of {origin.Name}" : size >= 3 ? "City" : "Town";
        _plan = BuildDenseTown(rng, scene, size, name);
        FinishTown(rng, scene);
        Console.WriteLine($"CitySceneFactory: Built {name} — biome={Biome}, size={size}, {scene.AllAreas.Count} areas");
    }

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        SpawnQueuedCrews(rng, scene);
        // Twice the ways, twice the pigeons: the town animals are let loose over each half of the city.
        var outdoors = scene.OutdoorAreas;
        int half = outdoors.Count / 2;
        TownAnimals(rng, scene, outdoors.Take(half).ToList());
        TownAnimals(rng, scene, outdoors.Skip(half).ToList());
    }

    // ── The trades ───────────────────────────────────────────────────────────

    /// <summary>Every trade a city can house, each at most once: its shop's name, floor, master and noun.</summary>
    private static readonly (string Name, Func<Area> Hall, Func<CraftsmanArchetype> Master, string Noun)[] CityTrades =
    {
        ("Forge",                Shared.WorkshopSubfactory.BuildForge,             () => new BlacksmithArchetype(), "forge"),
        ("Carpenter's Workshop", Shared.WorkshopSubfactory.BuildCarpenterWorkshop, () => new CarpenterArchetype(),  "workshop"),
        ("Cooper's Workshop",    Shared.WorkshopSubfactory.BuildCooperWorkshop,    () => new CooperArchetype(),     "workshop"),
        ("Weaver's Workshop",    Shared.WorkshopSubfactory.BuildWeaverWorkshop,    () => new WeaverArchetype(),     "workshop"),
        ("Bakery",               Shared.WorkshopSubfactory.BuildBakery,            () => new BakerArchetype(),      "bakery"),
        ("Mill",                 Shared.WorkshopSubfactory.BuildMill,              () => new MillerArchetype(),     "mill"),
        ("Cobbler's Shop",       Shared.CityTradeSubfactory.BuildCobblery,         () => new CobblerArchetype(),    "shop"),
        ("Tailor's Shop",        Shared.CityTradeSubfactory.BuildTailory,          () => new TailorArchetype(),     "shop"),
        ("Chandlery",            Shared.CityTradeSubfactory.BuildChandlery,        () => new ChandlerArchetype(),   "chandlery"),
        ("Butcher's Shop",       Shared.CityTradeSubfactory.BuildButchery,         () => new ButcherArchetype(),    "shop"),
        ("Tannery",              Shared.CityTradeSubfactory.BuildTannery,          () => new TannerArchetype(),     "tannery"),
        ("Pottery",              Shared.CityTradeSubfactory.BuildPottery,          () => new PotterArchetype(),     "pottery"),
        ("Apothecary's Shop",    Shared.CityTradeSubfactory.BuildDispensary,       () => new ApothecaryArchetype(), "shop"),
        ("Barber's Shop",        Shared.CityTradeSubfactory.BuildBarbershop,       () => new BarberArchetype(),     "shop"),
    };

    private static readonly string[] MerchantHouses = { "Merchant House", "Counting House", "Wool Exchange", "Spice House", "Cloth Exchange" };

    /// <summary>What a dwelling is called from the street, beside its weathering: "Sagging Tenement".</summary>
    private static readonly string[] DwellingNouns = { "House", "Tenement", "Townhouse", "Cottage", "Lodging House" };

    // ── The dense town ───────────────────────────────────────────────────────

    /// <summary>
    /// The doors still free on each outdoor way. <see cref="Take"/> spends them; whatever is left once
    /// the public buildings stand is filled with houses.
    /// </summary>
    private sealed class Slots
    {
        private readonly Dictionary<Area, int> _left = new();
        private readonly List<Area> _order;
        private readonly Random _rng;

        public Slots(IReadOnlyList<Area> ways, Random rng)
        {
            _rng = rng;
            _order = ways.ToList();
            foreach (var w in ways) _left[w] = MinDoorsPerWay + (rng.NextDouble() < FifthDoorChance ? 1 : 0);
        }

        public int Remaining => _left.Values.Sum();

        /// <summary>A free door on one of <paramref name="prefer"/>, else on any way with a door free.</summary>
        public Area Take(IEnumerable<Area> prefer)
        {
            var open = prefer.Where(a => _left.GetValueOrDefault(a) > 0).ToList();
            if (open.Count == 0) open = _order.Where(a => _left[a] > 0).ToList();
            if (open.Count == 0) return _order[_rng.Next(_order.Count)];   // more buildings than doors: never by construction
            var at = open[_rng.Next(open.Count)];
            _left[at]--;
            return at;
        }
    }

    private Shared.CityPlan BuildDenseTown(Random rng, Scene scene, int size, string name)
    {
        var plan = Shared.CityLayout.BuildDense(scene, rng, Biome, size, name);
        RegisterAll(scene, plan.Section);
        var ways     = plan.All;
        var squares  = plan.Squares;
        var front    = plan.Frontages;
        var streets  = plan.Streets;
        var back     = plan.Alleys.Concat(plan.Streets).ToList();
        var gate     = plan.Gateway!;
        var slots    = new Slots(ways, rng);

        var dwellingNames = BuildingDescriptions.BuildingWear
            .SelectMany(w => DwellingNouns.Select(n => (Wear: w, Noun: n)))
            .OrderBy(_ => rng.Next()).ToList();
        int dwelling = 0;

        BuildingResult Put(string label, string noun, BuildingAccess access, BuildingOccupancy occ, int beds, Area at,
                           Func<Area>? hall = null, string? wear = null)
        {
            var b = BuildingFactory.Build(new BuildingSpec
            {
                BuildingName      = label,
                RoomPrefix        = label,
                Access            = access,
                Occupancy         = occ,
                OutsideArea       = at,
                BedCount          = beds,
                Material          = RollMaterial(rng),
                FunctionNoun      = noun,
                PublicHallBuilder = hall,
                ExteriorWear      = wear,
            }, rng);
            RegisterBuilding(scene, b);
            return b;
        }

        BuildingResult Dwelling(int beds, Area at)
        {
            var (wear, noun) = dwellingNames[dwelling++ % dwellingNames.Count];
            string label = $"{Capitalise(wear)} {noun}";
            return Put(label, noun.ToLowerInvariant(), BuildingAccess.Private, BuildingOccupancy.Individual, beds, at, wear: wear);
        }

        Func<int, NamedNpcArchetype, Area, Random, NpcSchedule> AtWork(Area hall, int away)
            => (i, _, bed, r) => i == 0
                ? BuildingSchedule.ForWorker(bed, hall, ways, r, awayPeriods: away)
                : BuildingSchedule.ForWorker(bed, hall, ways, r, awayPeriods: 2);

        // The inns: always one on a square, a coaching inn too in a larger city.
        var innNames = new List<string> { "Inn" };
        if (size >= 2) innNames.Add("Coaching Inn");
        foreach (var innName in innNames)
        {
            var roster = new List<NamedNpcArchetype> { new InnkeeperArchetype() };
            var inn = Put(innName, "inn", BuildingAccess.Public, BuildingOccupancy.Communal, roster.Count, slots.Take(squares));
            Crews.Add(new Crew(roster, inn.BedAreas, inn.PublicHall.ContextDescription, AtWork(inn.PublicHall, 1),
                new[] { inn.Section }, inn.PublicHall));
        }

        // The alehouse and its brewer.
        {
            var ale = Put("Alehouse", "alehouse", BuildingAccess.Public, BuildingOccupancy.Individual, 1, slots.Take(back),
                          Shared.WorkshopSubfactory.BuildAlehouse);
            Crews.Add(new Crew(new NamedNpcArchetype[] { new BrewerArchetype() }, ale.BedAreas, ale.PublicHall.ContextDescription,
                AtWork(ale.PublicHall, 1), new[] { ale.Section }, ale.PublicHall));
        }

        // Merchants, each with a clerk.
        for (int m = 0; m < size + 1 && m < MerchantHouses.Length; m++)
        {
            var roster = new List<NamedNpcArchetype> { new MerchantArchetype(), new ClerkArchetype() };
            var house = Put(MerchantHouses[m], "merchant's house", BuildingAccess.Public, BuildingOccupancy.Communal, 2, slots.Take(front));
            Crews.Add(new Crew(roster, house.BedAreas, house.PublicHall.ContextDescription, AtWork(house.PublicHall, 1),
                new[] { house.Section }, house.PublicHall));
        }

        // The trades, each with an apprentice in a house of their own.
        int trades = Math.Min(CityTrades.Length, 8 + 2 * size);
        foreach (var idx in SampleUniqueIndices(rng, CityTrades.Length, trades))
        {
            var (label, hall, master, noun) = CityTrades[idx];
            var shop = Put(label, noun, BuildingAccess.Public, BuildingOccupancy.Individual, 1, slots.Take(streets), hall);
            var home = Dwelling(1, slots.Take(back));
            var m = master();
            var roster = new List<NamedNpcArchetype> { m, new ApprenticeArchetype { Master = m } };
            Crews.Add(new Crew(roster, new[] { shop.BedAreas[0], home.BedAreas[0] }, shop.PublicHall.ContextDescription,
                (i, _, bed, r) => i == 0
                    ? BuildingSchedule.ForWorker(bed, shop.PublicHall, ways, r, awayPeriods: r.NextDouble() < 0.5 ? 1 : 2)
                    : BuildingSchedule.ForWorker(bed, shop.PublicHall, ways.Append(home.PublicHall).ToList(), r, awayPeriods: 2),
                new[] { shop.Section }, shop.PublicHall));
        }

        // The watch, at the gate.
        {
            var watch = new List<NamedNpcArchetype>();
            if (size >= 2) watch.Add(new CaptainArchetype());
            for (int i = 0, n = 2 + 2 * size; i < n; i++) watch.Add(new GuardArchetype());
            var watchHouse = Put("Watch House", "watch house", BuildingAccess.Public, BuildingOccupancy.Communal, watch.Count, slots.Take(new[] { gate }));
            Crews.Add(new Crew(watch, watchHouse.BedAreas, watchHouse.PublicHall.ContextDescription,
                (i, _, bed, r) => i == 0
                    ? BuildingSchedule.ForWorker(bed, watchHouse.PublicHall, ways, r, awayPeriods: 2)
                    : BuildingSchedule.ForHand(bed, ways.Append(watchHouse.PublicHall).ToList(), r),
                new[] { watchHouse.Section }, watchHouse.PublicHall));
        }

        // The wash house, where the laundresses live and work.
        {
            var roster = new List<NamedNpcArchetype>();
            for (int i = 0, n = 2 + rng.Next(0, 2); i < n; i++) roster.Add(new LaundressArchetype());
            var wash = Put("Wash House", "wash house", BuildingAccess.Public, BuildingOccupancy.Communal, roster.Count, slots.Take(front),
                           Shared.CityTradeSubfactory.BuildWashHouse);
            Crews.Add(new Crew(roster, wash.BedAreas, wash.PublicHall.ContextDescription, AtWork(wash.PublicHall, 1),
                new[] { wash.Section }, wash.PublicHall));
        }

        // The doss house, where the beggars sleep when they have the copper; by day they are in the squares.
        {
            var roster = new List<NamedNpcArchetype>();
            for (int i = 0, n = 1 + size + rng.Next(0, 2); i < n; i++) roster.Add(new BeggarArchetype());
            var doss = Put("Doss House", "doss house", BuildingAccess.Private, BuildingOccupancy.Communal, roster.Count,
                           slots.Take(plan.Alleys));
            var begging = squares.Append(gate).Concat(plan.Alleys).ToList();
            Crews.Add(new Crew(roster, doss.BedAreas, doss.PublicHall.ContextDescription,
                (_, _, bed, r) => BuildingSchedule.ForHand(bed, begging, r),
                new[] { doss.Section }));
        }

        // The townsfolk: every door still free is a house with one or two of them in it.
        var wells = squares.Concat(streets).ToList();
        var counting = PublicHallsNamed(scene, MerchantHouses);
        while (slots.Remaining > 0)
        {
            var roster = new List<NamedNpcArchetype>();
            for (int i = 0, n = rng.NextDouble() < 0.5 ? 1 : 2; i < n; i++) roster.Add(Townsman(rng));
            var house = Dwelling(roster.Count, slots.Take(ways));
            Crews.Add(new Crew(roster, house.BedAreas, house.PublicHall.ContextDescription,
                (_, who, bed, r) => who switch
                {
                    WaterCarrierArchetype => BuildingSchedule.ForHand(bed, wells, r),
                    ClerkArchetype when counting.Count > 0
                        => BuildingSchedule.ForWorker(bed, counting[r.Next(counting.Count)], ways, r, awayPeriods: 2),
                    _ => BuildingSchedule.ForHand(bed, ways, r),
                },
                new[] { house.Section }));
        }

        return plan;
    }

    /// <summary>One of the people a city house is lived in by.</summary>
    private static NamedNpcArchetype Townsman(Random rng)
    {
        double roll = rng.NextDouble();
        return roll < 0.45 ? new PorterArchetype()
             : roll < 0.75 ? new WaterCarrierArchetype()
             : new ClerkArchetype();
    }

    /// <summary>The public halls of the buildings already standing under any of <paramref name="names"/>.</summary>
    private static List<Area> PublicHallsNamed(Scene scene, IEnumerable<string> names)
    {
        var wanted = names.ToHashSet();
        return scene.Sections.Where(s => wanted.Contains(s.DisplayName))
                    .Select(s => s.Areas.FirstOrDefault(a => !a.IsPrivate))
                    .Where(a => a != null).Select(a => a!).ToList();
    }

    private static string Capitalise(string s)
        => string.Join('-', s.Split('-').Select(p => p.Length == 0 ? p : char.ToUpperInvariant(p[0]) + p[1..]));
}
