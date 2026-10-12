using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.History;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Building;
using Cathedral.Glyph.Microworld;

namespace Cathedral.Game.Scene.Settled;

/// <summary>
/// Base for the factories of the settled country — farmland, stock, settlements, cities and the places
/// history built. Three things they all need and none should write twice:
/// <list type="bullet">
/// <item><b>The ground they stand on.</b> A location keeps the biome of its cell, and that biome decides
/// what its buildings are made of (<see cref="BuildingDescriptions.MaterialsOf"/>). Read from
/// <see cref="WorldSites.BiomeAt"/>; with no world behind the build — the audits — it is
/// <see cref="DefaultBiome"/>, or whatever biome the factory was constructed for.</item>
/// <item><b>The place history put here</b>, when there is one: its name and its holder, through
/// <see cref="WorldSites.At"/>.</item>
/// <item><b>A crew housed and scheduled</b> — drawn up first, then given beds, then given a day — the
/// rule every factory since the field's longhouse has followed.</item>
/// </list>
/// </summary>
public abstract class SettledSceneFactory : SceneFactory
{
    private readonly string? _forcedBiome;

    protected SettledSceneFactory(string? biome = null, string? sessionPath = null) : base(sessionPath)
        => _forcedBiome = biome;

    /// <summary>The biome this place is built for when neither the constructor nor the world says.</summary>
    protected abstract string DefaultBiome { get; }

    /// <summary>The biome of the cell being built. Valid from the start of <see cref="BuildSections"/>.</summary>
    protected string Biome { get; private set; } = "plain";

    /// <summary>The settled location at this cell, when a world is published.</summary>
    protected SiteLocation? Site { get; private set; }

    protected sealed override void BuildSections(Random rng, int locationId, Scene scene)
    {
        Biome = _forcedBiome ?? WorldSites.BiomeAt(locationId) ?? DefaultBiome;
        Site  = WorldSites.At(locationId);
        BuildPlace(rng, locationId, scene);
    }

    /// <summary>Builds the sections, areas and buildings. <see cref="Biome"/> and <see cref="Site"/> are set.</summary>
    protected abstract void BuildPlace(Random rng, int locationId, Scene scene);

    /// <summary>A building material this climate builds in.</summary>
    protected BuildingMaterial RollMaterial(Random rng)
    {
        var pool = BuildingDescriptions.MaterialsOf(Biome);
        return pool[rng.Next(pool.Length)];
    }

    /// <summary>The first biome whose column of <paramref name="table"/> lists <paramref name="key"/>.</summary>
    protected static string FirstBiomeListing(IReadOnlyDictionary<string, string[]> table, string key, string fallback = "plain")
        => table.FirstOrDefault(kv => kv.Value.Contains(key)).Key ?? fallback;

    // ── Points of interest ────────────────────────────────────────────────────

    /// <summary>A PoI's lessons, verb to modus mentis, for its <c>VerbModiMentis</c> initializer.</summary>
    protected static IReadOnlyDictionary<string, string> Teach(params (string verb, string mm)[] lessons)
        => lessons.ToDictionary(l => l.verb, l => l.mm);

    /// <summary>A list of item elements from item factories.</summary>
    protected static List<ItemElement> Items(params Func<Item>[] items)
        => items.Select(f => new ItemElement(f())).ToList();

    // ── Crews ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Houses <paramref name="roster"/>: a public communal hall for up to five, owned by the first of
    /// them, and a private bunkhouse for the rest. Returns the beds in roster order.
    /// </summary>
    protected (BuildingResult Hall, BuildingResult? Bunkhouse, List<Area> Beds) HouseCrew(
        Random rng, Scene scene, IReadOnlyList<NamedNpcArchetype> roster, Area front, IReadOnlyList<Area> outdoors,
        string hallName, string hallNoun, string bunkName = "Bunkhouse")
    {
        int hallBeds = Math.Min(roster.Count, 5);
        var hall = BuildingFactory.Build(new BuildingSpec
        {
            BuildingName = hallName,
            RoomPrefix   = hallName,
            Access       = BuildingAccess.Public,
            Occupancy    = BuildingOccupancy.Communal,
            OutsideArea  = front,
            BedCount     = Math.Max(1, hallBeds),
            Material     = RollMaterial(rng),
            FunctionNoun = hallNoun,
        }, rng);
        RegisterBuilding(scene, hall);

        BuildingResult? bunk = null;
        if (roster.Count > hallBeds)
        {
            bunk = BuildingFactory.Build(new BuildingSpec
            {
                BuildingName = bunkName,
                RoomPrefix   = bunkName,
                Access       = BuildingAccess.Private,
                Occupancy    = BuildingOccupancy.Communal,
                OutsideArea  = Shared.OutdoorLayout.DistributeEntrances(outdoors, 1, rng)[0],
                BedCount     = roster.Count - hallBeds,
                Material     = RollMaterial(rng),
                FunctionNoun = "bunkhouse",
            }, rng);
            RegisterBuilding(scene, bunk);
        }

        var beds = hall.BedAreas.Concat(bunk?.BedAreas ?? Array.Empty<Area>()).ToList();
        return (hall, bunk, beds);
    }

    /// <summary>
    /// Spawns one named NPC with a schedule. <paramref name="owns"/> are the sections the NPC holds —
    /// the master of a place owns its buildings, so a witness check indoors defers to them.
    /// </summary>
    protected NpcEntity SpawnResident(Random rng, Scene scene, NamedNpcArchetype archetype, string context,
                                      NpcSchedule schedule, params Section[] owns)
    {
        var entity = archetype.Spawn(rng, context, _locationState != null ? _locationState.AffinityFor : null);
        foreach (var s in owns) entity.OwnedSectionIds.Add(s.Id.ToString());
        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = schedule;
        return entity;
    }

    /// <summary>
    /// The usual crew day: the first of the roster is the master, at the hall with one period out among
    /// <paramref name="work"/>; everyone else works <paramref name="work"/> (each by their own list
    /// when <paramref name="workOf"/> gives one); the hall is covered by the hands. Night is always the
    /// sleeper's own bed.
    ///
    /// <para><b>With a <paramref name="store"/></b> the place trades from it rather than from the hall:
    /// it is every trader's premises, the master spends their one period out in it, every hand who
    /// trades is in it at least once a day, and the trading hands keep it manned at every day period —
    /// staffed before the hall, and neither staffing pass pulls anyone off the other place. Without
    /// one the hall is where its people trade.</para>
    /// </summary>
    protected void SpawnCrew(Random rng, Scene scene, IReadOnlyList<NamedNpcArchetype> roster, IReadOnlyList<Area> beds,
                             Area hall, IReadOnlyList<Area> work, string context, Section[] masterOwns,
                             Func<NamedNpcArchetype, IReadOnlyList<Area>?>? workOf = null, Area? store = null)
    {
        var premises = store ?? hall;
        var hands    = new List<NpcSchedule>();
        var keepers  = new List<NpcSchedule>();
        var locks    = new StaffingLocks();
        for (int i = 0; i < roster.Count && i < beds.Count; i++)
        {
            var who = roster[i];
            if (i == 0)
            {
                // Elsewhere is everywhere but the hall itself, or the one period out could be spent in it.
                var away = store != null && IsTrader(who)
                    ? new List<Area> { store }
                    : work.Where(a => a.Id != hall.Id).ToList();
                var day = BuildingSchedule.ForWorker(beds[i], hall, away, rng, awayPeriods: 1);
                TradeAt(SpawnResident(rng, scene, who, context, day, masterOwns), premises);
            }
            else
            {
                var day = BuildingSchedule.ForHand(beds[i], workOf?.Invoke(who) ?? work, rng);
                if (IsTrader(who))
                {
                    BuildingSchedule.EnsureVisit(day, premises, rng, locks);
                    keepers.Add(day);
                }
                hands.Add(day);
                TradeAt(SpawnResident(rng, scene, who, context, day), premises);
            }
        }
        if (store != null)
        {
            scene.Stores.Add(store);
            BuildingSchedule.StaffPublicHall(store, keepers, locks);
        }
        if (hands.Count > 0) BuildingSchedule.StaffPublicHall(hall, hands, locks);
    }

    /// <summary>Whether people of this kind trade at all — the roster is drawn before anyone is spawned.</summary>
    protected static bool IsTrader(NamedNpcArchetype who) => who.SellTag != null || who.BuyTag != null;

    // ── Queued crews ──────────────────────────────────────────────────────────

    /// <summary>
    /// People waiting to be spawned: drawn up and housed during <see cref="BuildPlace"/>, given their
    /// day in <see cref="SpawnQueuedCrews"/>. <paramref name="Day"/> builds each one's schedule from
    /// their place in the roster and their bed.
    ///
    /// <para>Where they trade: <paramref name="Store"/> when the crew has one, which every trader of
    /// it then keeps (see <see cref="SpawnCrew"/>); otherwise whatever <paramref name="TradesAt"/>
    /// names for each of them — a shop's counter for its master and apprentice, the entrance of a
    /// great building for its steward. A trader given neither trades nowhere.</para>
    /// </summary>
    protected sealed record Crew(
        IReadOnlyList<NamedNpcArchetype> Roster, IReadOnlyList<Area> Beds, string Context,
        Func<int, NamedNpcArchetype, Area, Random, NpcSchedule> Day, Section[] MasterOwns, Area? Hall = null,
        Func<NamedNpcArchetype, Area?>? TradesAt = null, Area? Store = null);

    protected readonly List<Crew> Crews = new();

    /// <summary>
    /// Spawns every queued crew; a crew with a hall has it covered by everyone but its master, and a
    /// crew with a store has it kept by its traders first.
    /// </summary>
    protected void SpawnQueuedCrews(Random rng, Scene scene)
    {
        foreach (var crew in Crews)
        {
            var cover   = new List<NpcSchedule>();
            var keepers = new List<NpcSchedule>();
            var locks   = new StaffingLocks();
            for (int i = 0; i < crew.Roster.Count && i < crew.Beds.Count; i++)
            {
                var who      = crew.Roster[i];
                var day      = crew.Day(i, who, crew.Beds[i], rng);
                var premises = crew.Store ?? crew.TradesAt?.Invoke(who);
                if (premises != null && IsTrader(who))
                {
                    BuildingSchedule.EnsureVisit(day, premises, rng, locks);
                    if (i > 0) keepers.Add(day);
                }
                if (i > 0) cover.Add(day);
                var entity = SpawnResident(rng, scene, who, crew.Context, day, i == 0 ? crew.MasterOwns : Array.Empty<Section>());
                if (premises != null) TradeAt(entity, premises);
            }
            if (crew.Store != null)
            {
                scene.Stores.Add(crew.Store);
                BuildingSchedule.StaffPublicHall(crew.Store, keepers, locks);
            }
            if (crew.Hall != null && cover.Count > 0)
                BuildingSchedule.StaffPublicHall(crew.Hall, cover, locks);
        }
    }

    // ── Towns ─────────────────────────────────────────────────────────────────

    private static readonly (string Name, Func<Area> Hall, Func<CraftsmanArchetype> Master, string Noun)[] Trades =
    {
        ("Forge",                Shared.WorkshopSubfactory.BuildForge,             () => new BlacksmithArchetype(), "forge"),
        ("Carpenter's Workshop", Shared.WorkshopSubfactory.BuildCarpenterWorkshop, () => new CarpenterArchetype(),  "workshop"),
        ("Cooper's Workshop",    Shared.WorkshopSubfactory.BuildCooperWorkshop,    () => new CooperArchetype(),     "workshop"),
        ("Weaver's Workshop",    Shared.WorkshopSubfactory.BuildWeaverWorkshop,    () => new WeaverArchetype(),     "workshop"),
        ("Bakery",               Shared.WorkshopSubfactory.BuildBakery,            () => new BakerArchetype(),      "bakery"),
        ("Mill",                 Shared.WorkshopSubfactory.BuildMill,              () => new MillerArchetype(),     "mill"),
    };

    /// <summary>
    /// The streets of a town of <paramref name="size"/> (1..3) and the people who keep its trades: an
    /// inn, merchants' houses, workshops each with an apprentice housed nearby, and a watch house.
    /// Everything is built in the local material and queued in <see cref="Crews"/>. Returns the plan,
    /// whose squares are where a great building's door should open.
    /// </summary>
    protected Shared.CityPlan BuildTown(Random rng, Scene scene, int size, string name, bool harbour = false)
    {
        var plan = Shared.CityLayout.Build(scene, rng, Biome, size, name);
        RegisterAll(scene, plan.Section);
        var streets = plan.All;
        var front   = plan.Frontages;

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

        Func<int, NamedNpcArchetype, Area, Random, NpcSchedule> AtWork(Area hall, int away)
            => (i, _, bed, r) => i == 0
                ? BuildingSchedule.ForWorker(bed, hall, streets, r, awayPeriods: away)
                : BuildingSchedule.ForWorker(bed, hall, streets, r, awayPeriods: 2);

        // The inn: an innkeeper and, in a port, a sailor or two lodging there between ships.
        var innRoster = new List<NamedNpcArchetype> { new InnkeeperArchetype() };
        if (harbour) for (int i = 0, n = rng.Next(1, 3); i < n; i++) innRoster.Add(new SailorArchetype());
        var inn = Put(harbour ? "Harbour Inn" : "Inn", "inn", BuildingAccess.Public, BuildingOccupancy.Communal,
                      innRoster.Count, plan.Squares[0]);
        Crews.Add(new Crew(innRoster, inn.BedAreas, inn.PublicHall.ContextDescription,
            (i, who, bed, r) => i == 0 || who is not SailorArchetype
                ? AtWork(inn.PublicHall, 1)(i, who, bed, r)
                : BuildingSchedule.ForHand(bed, streets, r),
            new[] { inn.Section }, inn.PublicHall, TradesAt: _ => inn.PublicHall));

        // Merchants, each with a clerk.
        var entrances = Shared.OutdoorLayout.DistributeEntrances(front, size + 3, rng);
        int e = 0;
        string[] houses = { "Merchant House", "Counting House", "Wool Exchange", "Spice House", "Cloth Exchange" };
        for (int m = 0; m < size; m++)
        {
            var roster = new List<NamedNpcArchetype> { new MerchantArchetype(), new ClerkArchetype() };
            var house = Put(houses[m % houses.Length], "merchant's house", BuildingAccess.Public, BuildingOccupancy.Communal, 2, entrances[e++]);
            Crews.Add(new Crew(roster, house.BedAreas, house.PublicHall.ContextDescription, AtWork(house.PublicHall, 1),
                new[] { house.Section }, house.PublicHall, TradesAt: _ => house.PublicHall));
        }

        // Workshops, each with an apprentice in a house of their own.
        var wear = BuildingDescriptions.BuildingWear.OrderBy(_ => rng.Next()).ToList();
        int w = 0;
        foreach (var idx in SampleUniqueIndices(rng, Trades.Length, Math.Min(Trades.Length, size + 1)))
        {
            var (label, hall, master, noun) = Trades[idx];
            var shop = Put(label, noun, BuildingAccess.Public, BuildingOccupancy.Individual, 1,
                           entrances[e++ % entrances.Count], hall);
            string trait = wear[w++ % wear.Count];
            var home = Put(BuildingDescriptions.AnonymousLabel(trait, BuildingKind.House), "house", BuildingAccess.Private,
                           BuildingOccupancy.Individual, 1, Shared.OutdoorLayout.DistributeEntrances(streets, 1, rng)[0], wear: trait);
            var m = master();
            var roster = new List<NamedNpcArchetype> { m, new ApprenticeArchetype { Master = m } };
            Crews.Add(new Crew(roster, new[] { shop.BedAreas[0], home.BedAreas[0] }, shop.PublicHall.ContextDescription,
                (i, _, bed, r) => i == 0
                    ? BuildingSchedule.ForWorker(bed, shop.PublicHall, streets, r, awayPeriods: r.NextDouble() < 0.5 ? 1 : 2)
                    : BuildingSchedule.ForWorker(bed, shop.PublicHall, streets.Append(home.PublicHall).ToList(), r, awayPeriods: 2),
                new[] { shop.Section }, shop.PublicHall, TradesAt: _ => shop.PublicHall));
        }

        // The watch.
        var watch = new List<NamedNpcArchetype>();
        if (size >= 3) watch.Add(new CaptainArchetype());
        for (int i = 0, n = 1 + size; i < n; i++) watch.Add(new GuardArchetype());
        var gate = plan.Gateway ?? plan.Squares[0];
        var watchHouse = Put("Watch House", "watch house", BuildingAccess.Public, BuildingOccupancy.Communal, watch.Count, gate);
        Crews.Add(new Crew(watch, watchHouse.BedAreas, watchHouse.PublicHall.ContextDescription,
            (i, _, bed, r) => i == 0
                ? BuildingSchedule.ForWorker(bed, watchHouse.PublicHall, streets, r, awayPeriods: 2)
                : BuildingSchedule.ForHand(bed, streets.Append(watchHouse.PublicHall).ToList(), r),
            new[] { watchHouse.Section }, watchHouse.PublicHall));

        return plan;
    }

    /// <summary>Furnishes a town's streets and lets the usual town animals loose in them.</summary>
    protected void FinishTown(Random rng, Scene scene)
    {
        var outdoors = scene.OutdoorAreas;
        Shared.FurnitureSubfactory.AddSitSpots(rng, outdoors, Shared.FurnitureSubfactory.Setting.Settlement);
        Shared.FurnitureSubfactory.AddHidingPlaces(rng, outdoors, Shared.FurnitureSubfactory.Setting.Settlement);
        Shared.FurnitureSubfactory.AddShortcuts(rng, scene, outdoors, Shared.FurnitureSubfactory.Setting.Settlement);
        Shared.FurnitureSubfactory.AddExtractionPoints(rng, outdoors, Shared.FurnitureSubfactory.Setting.Settlement);
        AddRoofLandscapes(scene);
    }

    /// <summary>The pigeons, rats, starlings and jackdaws every town has.</summary>
    protected static void TownAnimals(Random rng, Scene scene, IReadOnlyList<Area> streets)
    {
        TrySpawnShallow(rng, scene, new PigeonArchetype(), streets, 0.9);
        TrySpawnShallow(rng, scene, new PigeonArchetype(), streets, 0.6);
        TrySpawnShallow(rng, scene, new StarlingArchetype(), streets, 0.5);
        TrySpawnShallow(rng, scene, new MagpieArchetype(), streets, 0.3);
        TrySpawnShallow(rng, scene, new RatArchetype(), streets, 0.5);
        TrySpawnShallow(rng, scene, new JackdawArchetype(), streets, 0.4);
        SprinkleSmallLife(rng, scene, streets, SmallLife.Urban, 2, 5);
    }

    /// <summary>A few shallow animals of one kind, kept in <paramref name="home"/> all day.</summary>
    protected static void Keep(Random rng, Scene scene, Func<ShallowNpcArchetype> kind, Area home, int min, int max)
    {
        int n = rng.Next(min, max + 1);
        for (int i = 0; i < n; i++) SpawnShallow(rng, scene, kind(), home);
    }
}
