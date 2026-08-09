using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Shared;
using Cathedral.Fight.Generators;

namespace Cathedral.Game.Scene.Village;

/// <summary>
/// Builds a procedural village: a small outdoor green, and a <b>building</b> for every craftsman.
///
/// <para>Outdoors: Square (hub), Craft Row and Market End, joined by paths. Each of those is where a
/// cluster of buildings is entered from.</para>
///
/// <para>Buildings: Forge and Mill always, then 2–4 more from the optional pool. Each is a
/// public+individual <b>workshop</b> — the existing <see cref="WorkshopSubfactory"/> area becomes its
/// public hall, and the master sleeps upstairs or out back. Every apprentice gets a
/// private+individual <b>house</b> off the Square.</para>
///
/// <para>This replaces a flat layout where a workshop was a single outdoor-adjacent area, and every
/// villager in the place shared one anonymous dormitory. Craftsmen Hall and Sleeping Quarters are
/// gone; nobody sleeps in a room they do not own.</para>
/// </summary>
public class VillageSceneFactory : SceneFactory
{
    public VillageSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private Area? _square;
    private LayoutShape _layout;

    /// <summary>Every workshop building, with the archetype that masters it.</summary>
    private readonly List<(BuildingResult Building, CraftsmanArchetype Master)> _workshops = new();

    /// <summary>Houses built for apprentices, paired with the workshop they are bound to.</summary>
    private readonly List<(BuildingResult House, BuildingResult Workshop, CraftsmanArchetype MasterTrade)> _houses = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // ── 1. The outdoor village ───────────────────────────────────────────
        // The square is always there; how many ways lead off it, what they are called and how they
        // join up is rolled, so two villages are laid out differently rather than differing only in
        // which workshops they happen to contain.

        _square = BuildSquare(rng);
        var outdoorAreas = new List<Area> { _square };

        int lanes = rng.Next(1, 4);
        foreach (var idx in SampleUniqueIndices(rng, LanePool.Length, lanes))
        {
            var (name, lemma, context, transition, description, moods) = LanePool[idx];
            outdoorAreas.Add(new Area(name, lemma, context, transition, new() { description }, moods));
        }

        var outdoors = new Section(
            "Village",
            new() { "A cluster of workshops and houses around an open square" },
            seed => new GeometricGenerator { Seed = seed }
        );
        foreach (var area in outdoorAreas) outdoors.Areas.Add(area);
        scene.Sections.Add(outdoors);
        RegisterAll(scene, outdoors);

        _layout = OutdoorLayout.RollShape(rng);
        OutdoorLayout.Connect(scene, outdoorAreas, _layout, "Lane", rng);

        // ── 2. Which workshops ───────────────────────────────────────────────

        var trades = new List<(string Name, Func<Area> Hall, CraftsmanArchetype Master, string Noun, BuildingMaterial? Mat)>
        {
            ("Carpenter's Workshop", WorkshopSubfactory.BuildCarpenterWorkshop, new CarpenterArchetype(), "workshop", null),
            ("Cooper's Workshop",    WorkshopSubfactory.BuildCooperWorkshop,    new CooperArchetype(),    "workshop", null),
            ("Weaver's Workshop",    WorkshopSubfactory.BuildWeaverWorkshop,    new WeaverArchetype(),    "workshop", null),
            ("Bakery",               WorkshopSubfactory.BuildBakery,            new BakerArchetype(),     "bakery",   BuildingMaterial.Stone),
            ("Alehouse",             WorkshopSubfactory.BuildAlehouse,          new BrewerArchetype(),    "alehouse", null),
        };

        var chosen = new List<(string Name, Func<Area> Hall, CraftsmanArchetype Master, string Noun, BuildingMaterial? Mat)>
        {
            ("Forge", WorkshopSubfactory.BuildForge, new BlacksmithArchetype(), "forge", BuildingMaterial.Stone),
            ("Mill",  WorkshopSubfactory.BuildMill,  new MillerArchetype(),     "mill",  BuildingMaterial.Stone),
        };
        foreach (var idx in SampleUniqueIndices(rng, trades.Count, rng.Next(2, 5)))
            chosen.Add(trades[idx]);

        // ── 3. Where they stand ──────────────────────────────────────────────
        // Which lane a workshop opens onto is rolled too, so the forge is not always on the same
        // street. Every lane gets a building before any lane gets a second, so a rolled lane is never
        // an empty dead end.

        var workshopEntrances = OutdoorLayout.DistributeEntrances(outdoorAreas, chosen.Count, rng);
        for (int i = 0; i < chosen.Count; i++)
        {
            var (name, hall, master, noun, mat) = chosen[i];
            AddWorkshop(scene, rng, name, hall, master, workshopEntrances[i], noun, mat);
        }

        // ── 4. Apprentice houses ─────────────────────────────────────────────
        // Every workshop gets at least one apprentice, and this is a structural requirement rather
        // than flavour: a master must be out of their own shop for part of the day, and the shop
        // counter must be manned at every daylight hour, so somebody has to cover. A second
        // apprentice is a roll — with two, the cover rotates and the pair sometimes overlap with the
        // master in the hall.
        //
        // Each keeps their own roof — and the roof is anonymous. A workshop can hang a sign, so it is
        // named for its trade; a house cannot, so it is known only by how it looks from the street
        // ("the crooked house"). Naming these "Blacksmith Apprentice's House" told the player who
        // lived there before they had met a soul. The traits are drawn without replacement so the
        // houses stay tellable apart — the observation phase lists them side by side.

        var wear = BuildingDescriptions.BuildingWear.OrderBy(_ => rng.Next()).ToList();
        int nextWear = 0;

        foreach (var (workshop, master) in _workshops.ToList())
        {
            int apprentices = rng.NextDouble() < 0.35 ? 2 : 1;
            for (int i = 0; i < apprentices; i++)
            {
                string trait = wear[nextWear++ % wear.Count];
                string label = BuildingDescriptions.AnonymousLabel(trait, BuildingKind.House);

                var house = BuildingFactory.Build(new BuildingSpec
                {
                    BuildingName = label,
                    RoomPrefix   = label,
                    Access       = BuildingAccess.Private,
                    Occupancy    = BuildingOccupancy.Individual,
                    OutsideArea  = OutdoorLayout.DistributeEntrances(outdoorAreas, 1, rng)[0],
                    BedCount     = 1,
                    FunctionNoun = "house",
                    ExteriorWear = trait,
                }, rng);

                RegisterBuilding(scene, house);
                _houses.Add((house, workshop, master));
            }
        }

        Console.WriteLine($"VillageSceneFactory: village built — layout={_layout}, {outdoorAreas.Count} outdoor area(s), "
                        + $"{_workshops.Count} workshop(s), {_houses.Count} house(s), {scene.AllAreas.Count} areas");
    
        // ── Furnishing: somewhere to sit, somewhere to hide, a hard shortcut, a climb ──
        // Rolled, so two places of the same kind are not the same place. Runs after the sections and
        // paths exist: shortcuts need to know what is already adjacent, and the climb needs a section
        // to put its top area in.
        {
            var furnishable = scene.OutdoorAreas;
            FurnitureSubfactory.AddSitSpots(rng, furnishable, FurnitureSubfactory.Setting.Settlement);
            FurnitureSubfactory.AddHidingPlaces(rng, furnishable, FurnitureSubfactory.Setting.Settlement);
            FurnitureSubfactory.AddShortcuts(rng, scene, furnishable, FurnitureSubfactory.Setting.Settlement);
            FurnitureSubfactory.AddExtractionPoints(rng, furnishable, FurnitureSubfactory.Setting.Settlement);

        }

        // Every building has a roof, and from a roof you can see the rest of the outside.
        AddRoofLandscapes(scene);
}

    /// <summary>
    /// The ways that can lead off the square. A village draws 1–3 without replacement, so both how
    /// many streets it has and which ones vary. Deliberately no "market": business is done at a
    /// workshop counter, and a place called the market end implied trade happened there.
    /// </summary>
    private static readonly (string Name, string Lemma, string Context, string Transition, string Description, string[] Moods)[] LanePool =
    {
        ("Craft Row", "row", "on the village craft row", "walk down the craft row",
         "A rutted lane with workshop doors down both sides, loud with hammering and sawing",
         new[] { "noisy", "narrow", "busy", "rutted", "smoky" }),

        ("Mill Lane", "lane", "on mill lane", "walk down mill lane",
         "A broad cart-track worn into ruts by loaded waggons, pale with spilled flour",
         new[] { "broad", "dusty", "trafficked", "flour-pale" }),

        ("Church Walk", "walk", "on the church walk", "walk along the church walk",
         "A quieter way of packed earth, running past a low wall and a leaning yew",
         new[] { "quiet", "shaded", "walled", "still", "green" }),

        ("Back Lane", "lane", "on the back lane", "slip into the back lane",
         "A narrow way behind the houses, muddy underfoot and hung with washing",
         new[] { "narrow", "muddy", "close", "overlooked", "dim" }),

        ("Waterside", "waterside", "on the waterside", "walk down to the waterside",
         "A soft-verged track along the stream, alder-shaded and loud with water",
         new[] { "damp", "green", "murmuring", "soft-verged", "cool" }),

        ("Green Edge", "green", "at the village green's edge", "walk out to the green's edge",
         "The open edge of the village where the grazing begins, fenced with hurdles",
         new[] { "open", "grassy", "windy", "wide", "hurdle-fenced" }),
    };

    private void AddWorkshop(
        Scene scene, Random rng, string name, Func<Area> hallBuilder,
        CraftsmanArchetype master, Area outside, string functionNoun, BuildingMaterial? material)
    {
        var building = BuildingFactory.Build(new BuildingSpec
        {
            BuildingName          = name,
            RoomPrefix            = name,
            Access                = BuildingAccess.Public,
            Occupancy             = BuildingOccupancy.Individual,
            OutsideArea           = outside,
            PublicHallBuilder     = hallBuilder,
            BedCount              = 1,
            Material              = material,
            FunctionNoun          = functionNoun,
            ArenaGeneratorFactory = seed => new CorridorGenerator { Seed = seed },
        }, rng);

        RegisterBuilding(scene, building);
        _workshops.Add((building, master));
    }

    private static string Capitalise(string s)
        => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private static void ConnectViaPath(Scene scene, Area a, Area b, string pathName)
    {
        if (a == null || b == null) return;
        scene.ConnectAreasBidirectional(a, b);
        var path = new PathPointOfInterest(
            a, b, PathPointOfInterest.NameFor(a, b, pathName),
            new() { $"A {pathName.ToLowerInvariant()} running between {a.DisplayName.ToLowerInvariant()} and {b.DisplayName.ToLowerInvariant()}" },
            new[] { "worn", "muddy", "well-trodden" }
        );
        a.PointsOfInterest.Add(path);
        b.PointsOfInterest.Add(path);
        path.Register(scene);
    }

    // ── Outdoor area builders ───────────────────────────────────────────────

    private static Area BuildSquare(Random rng)
    {
        var square = new Area(
            displayName: "Square",
            referenceLemma: "square",
            contextDescription: "in the village square",
            transitionDescription: "step into the village square",
            descriptions: new() { "An open dirt square at the heart of the village, foot-traffic crossing every direction" },
            moods: new[] { "open", "noisy", "central", "muddy", "well-trodden" }
        );

        square.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Well",
            referenceLemma: "well",
            descriptions: new() { "A stone-rimmed well at the centre of the square, a wooden bucket on its rope" },
            items: new()
            {
                new ItemElement(new Rope()),
            },
            moods: new[] { "central", "stone-rimmed", "deep", "echoing" }
        ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "drainage", ["listen"] = "keen_ear" } });

        if (rng.NextDouble() < 0.40)
        {
            square.PointsOfInterest.Add(new PointOfInterest(
                displayName: "Market Stall",
                referenceLemma: "stall",
                descriptions: new() { "A trestle market-stall set out with goods for sale" },
                items: new()
                {
                    new ItemElement(new Bread()),
                    new ItemElement(new Ale()),
                    new ItemElement(new Cloth()),
                },
                moods: new[] { "bright", "cluttered", "bargain-shouted" }
            ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "bargaining", ["listen"] = "keen_ear", ["smell"] = "scenting" } });
        }

        return square;
    }

    private static Area BuildCraftRow() => new(
        displayName: "Craft Row",
        referenceLemma: "row",
        contextDescription: "on the village craft row",
        transitionDescription: "walk down the craft row",
        descriptions: new() { "A rutted lane with workshop doors down both sides, loud with hammering and sawing" },
        moods: new[] { "noisy", "narrow", "busy", "rutted", "smoky" }
    );

    private static Area BuildMarketEnd() => new(
        displayName: "Market End",
        referenceLemma: "market",
        contextDescription: "at the market end of the village",
        transitionDescription: "walk down to the market end",
        descriptions: new() { "The working end of the village, where carts are loaded and sacks come and go" },
        moods: new[] { "open", "dusty", "trafficked", "flour-pale" }
    );

    // ── NPC construction ────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_square is null) return;

        // Everywhere a villager might plausibly spend a period that is not their workplace: the open
        // village, and any public hall other than their own.
        var publicHalls = _workshops.Select(w => w.Building.PublicHall).ToList();
        // The outdoor section is whichever one holds the square — its area list is rolled, so it
        // cannot be reconstructed from fields the way a fixed layout could.
        var outdoorAreas = scene.Sections.First(s => s.Areas.Contains(_square)).Areas.ToList();

        var scheduleByWorkshop = new Dictionary<string, List<NpcSchedule>>();

        // ── Masters ─────────────────────────────────────────────────────────
        foreach (var (building, master) in _workshops)
        {
            var elsewhere = outdoorAreas
                .Concat(publicHalls.Where(h => h.Id != building.PublicHall.Id))
                .ToList();

            // A master is out of their own workshop for one or two periods — never none. That is the
            // rule the apprentice exists to serve: with the master always in, nobody would need to
            // cover, and the shop would have no reason to employ anyone.
            var schedule = BuildingSchedule.ForWorker(
                bed:         building.BedAreas.First(),
                workplace:   building.PublicHall,
                elsewhere:   elsewhere,
                rng:         rng,
                awayPeriods: rng.NextDouble() < 0.5 ? 1 : 2);

            SpawnInto(rng, scene, master, building, schedule);

            // The master is deliberately absent from the cover list — see StaffPublicHall.
            scheduleByWorkshop[building.Section.DisplayName] = new List<NpcSchedule>();
        }

        // ── Apprentices ─────────────────────────────────────────────────────
        foreach (var (house, workshop, masterTrade) in _houses)
        {
            var archetype = new ApprenticeArchetype { Master = masterTrade };

            var schedule = BuildingSchedule.ForWorker(
                bed:         house.BedAreas.First(),
                workplace:   workshop.PublicHall,
                elsewhere:   outdoorAreas.Concat(new[] { house.PublicHall }).ToList(),
                rng:         rng,
                awayPeriods: 2);

            SpawnInto(rng, scene, archetype, house, schedule);

            if (scheduleByWorkshop.TryGetValue(workshop.Section.DisplayName, out var staff))
                staff.Add(schedule);
        }

        // ── Keep every shop counter manned ──────────────────────────────────
        foreach (var (building, _) in _workshops)
        {
            if (scheduleByWorkshop.TryGetValue(building.Section.DisplayName, out var staff))
                BuildingSchedule.StaffPublicHall(building.PublicHall, staff);
        }
    
        // Small life. Every location has some; which and how many is rolled, so two
        // places of the same kind are not the same place.
        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Settlement, 2, 6);
}

    /// <summary>
    /// Spawns an NPC, gives them the section of the building they live in, and files their schedule.
    /// Owning the section is what makes them the authority there — <c>WitnessSelector</c> and
    /// <c>ThreatSelector</c> both prefer the owner over anyone else present, and until buildings
    /// existed no villager owned anything, so a thief in a workshop faced whoever happened to be
    /// standing nearest.
    /// </summary>
    private void SpawnInto(
        Random rng, Scene scene, NamedNpcArchetype archetype, BuildingResult home, NpcSchedule schedule)
    {
        var entity = archetype.Spawn(rng, home.PublicHall.ContextDescription,
            _locationState != null ? _locationState.AffinityFor : null);

        entity.OwnedSectionIds.Add(home.Section.Id.ToString());

        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = schedule;

        Console.WriteLine($"VillageSceneFactory: Spawned {entity.DisplayName} ({archetype.ArchetypeId}) in {home.Section.DisplayName}");
    }
}
