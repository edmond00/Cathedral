using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Shared;

namespace Cathedral.Game.Scene.Farm;

/// <summary>
/// Builds a complete medieval farm scene per the v1 world-content spec (farm.md).
///
/// Sections:
///   • Farmyard       — Courtyard (hub), Chicken Coop, Pigsty, Sheep Pen, Storage Shed, Dairy Shed
///   • Farm Grounds   — Vegetable Garden, Orchard
///   • Farmhouse      — Hall, Kitchen, optional Pantry, 1–3 Bedrooms (built by <see cref="HouseBuilder"/>)
///
/// Connections: <see cref="PathPointOfInterest"/> (Farmyard Tracks, Garden Paths) between
/// Courtyard and outdoor areas; locked <see cref="DoorPointOfInterest"/> from Courtyard → Hall.
///
/// NPCs (one per bedroom + role-specific): Farmer, Farmhand(s), Shepherd, Dairymaid,
/// Swineherd, Poultry Keeper. Shallow: Sheep, Pig, Chicken, Cow.
/// </summary>
public class FarmSceneFactory : SceneFactory
{
    public FarmSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private HouseResult? _houseResult;
    private Area? _courtyard, _chickenCoop, _pigsty, _sheepPen, _dairyShed, _shed;
    private Area? _vegetableGarden, _orchard;
    private bool  _hasSheep, _hasDairy;

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // ── 1. Decide farm composition ────────────────────────────────────────

        _hasSheep = rng.NextDouble() < 0.60;
        _hasDairy = rng.NextDouble() < 0.55;

        // ── 2. Build outdoor areas (all PoIs populated before registration) ──

        _courtyard       = BuildCourtyard();
        _chickenCoop     = AnimalPenSubfactory.BuildChickenCoop();
        _pigsty          = AnimalPenSubfactory.BuildPigsty();
        _vegetableGarden = BuildVegetableGarden(rng);
        _orchard         = BuildOrchard(rng);
        _shed            = BuildStorageShed();

        if (_hasSheep) _sheepPen  = AnimalPenSubfactory.BuildSheepPen();
        if (_hasDairy) _dairyShed = AnimalPenSubfactory.BuildDairyShed();

        // ── 3. Build sections in order so Courtyard is AllAreas[0] ────────────

        var farmyard = new Section(
            "Farmyard",
            new() { "The central yard of the farm, mud-churned and busy with animal sounds" }
        );
        farmyard.Areas.Add(_courtyard);
        farmyard.Areas.Add(_chickenCoop);
        farmyard.Areas.Add(_pigsty);
        farmyard.Areas.Add(_shed);
        if (_sheepPen  != null) farmyard.Areas.Add(_sheepPen);
        if (_dairyShed != null) farmyard.Areas.Add(_dairyShed);
        scene.Sections.Add(farmyard);
        RegisterAll(scene, farmyard);

        var grounds = new Section(
            "Farm Grounds",
            new() { "The working land around the farmhouse: gardens, orchards, and enclosures" }
        );
        grounds.Areas.Add(_vegetableGarden);
        grounds.Areas.Add(_orchard);
        scene.Sections.Add(grounds);
        RegisterAll(scene, grounds);

        // ── 4. Connect outdoor areas with PathPoIs (Courtyard as hub) ────────

        var outdoorPaths = new List<(Area, Area, string)>
        {
            (_courtyard, _chickenCoop, "Farmyard Track"),
            (_courtyard, _pigsty,      "Farmyard Track"),
            (_courtyard, _shed,        "Farmyard Track"),
            (_courtyard, _vegetableGarden, "Garden Path"),
            (_courtyard, _orchard,         "Garden Path"),
            (_vegetableGarden, _orchard,   "Garden Path"),
        };
        if (_sheepPen  != null) outdoorPaths.Add((_courtyard, _sheepPen,  "Farmyard Track"));
        if (_dairyShed != null) outdoorPaths.Add((_courtyard, _dairyShed, "Farmyard Track"));

        foreach (var (a, b, pathName) in outdoorPaths)
        {
            scene.ConnectAreasBidirectional(a, b);
            var path = new PathPointOfInterest(
                areaA: a,
                areaB: b,
                displayName: pathName,
                descriptions: new() { $"A worn farmyard track between the {a.DisplayName.ToLowerInvariant()} and the {b.DisplayName.ToLowerInvariant()}" },
                moods: new[] { "worn", "muddy", "narrow" }
            );
            a.PointsOfInterest.Add(path);
            b.PointsOfInterest.Add(path);
            path.Register(scene);
        }

        // ── 5. Build the farmhouse ───────────────────────────────────────────

        var houseBuilder = new HouseBuilder
        {
            MinBedrooms = 1,
            MaxBedrooms = rng.Next(1, 4),
            MaxFloors   = rng.NextDouble() < 0.65 ? 2 : 1,
        };
        var house = houseBuilder.Build(rng);
        HouseBuilder.PopulateFurniture(house, rng);

        foreach (var section in house.Sections)
        {
            foreach (var area in section.Areas)
                area.IsPrivate = true;

            scene.Sections.Add(section);
            RegisterAll(scene, section);
        }

        // ── 6. Main entrance: Courtyard ↔ Hall (locked door) ─────────────────

        var entranceDoor = BuildMainEntranceDoor(_courtyard, house.EntryRoom, house.Material);
        _courtyard.PointsOfInterest.Add(entranceDoor);
        house.EntryRoom.PointsOfInterest.Add(entranceDoor);
        entranceDoor.Register(scene);

        _houseResult = house;

        Console.WriteLine($"FarmSceneFactory: Built farm — sheep={_hasSheep} dairy={_hasDairy}, {house.Bedrooms.Count} bedroom(s)");
    }

    // ── NPC construction ────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_houseResult is null || _courtyard is null) return;

        var bedrooms = _houseResult.Bedrooms;
        var npcRoles = AssignBedroomRoles(rng, bedrooms.Count);

        for (int i = 0; i < bedrooms.Count; i++)
        {
            var bedroom   = bedrooms[i];
            var archetype = npcRoles[i];
            var entity    = SpawnNamed(rng, archetype, "a medieval farm");

            // The first NPC (farmer) owns the house sections
            if (i == 0)
            {
                foreach (var section in _houseResult.Sections)
                    entity.OwnedSectionIds.Add(section.Id.ToString());
            }

            var sceneNpc = new SceneNpc(entity);
            sceneNpc.Register(scene);
            scene.Npcs.Add(sceneNpc);
            scene.NpcSchedules[sceneNpc.Id] = BuildScheduleForRole(archetype.ArchetypeId, bedroom);

            Console.WriteLine($"FarmSceneFactory: Spawned {entity.DisplayName} ({archetype.ArchetypeId})");
        }

        // ── Shallow animals ─────────────────────────────────────────────────

        SpawnShallow(rng, scene, new ChickenArchetype(), _chickenCoop!, count: rng.Next(3, 7));
        SpawnShallow(rng, scene, new PigArchetype(),     _pigsty!,      count: rng.Next(1, 4));

        if (_sheepPen != null)
            SpawnShallow(rng, scene, new SheepArchetype(), _sheepPen, count: rng.Next(2, 7));
        if (_dairyShed != null)
            SpawnShallow(rng, scene, new CowArchetype(), _dairyShed, count: rng.Next(1, 3));
    }

    private List<NamedNpcArchetype> AssignBedroomRoles(Random rng, int count)
    {
        // Slot 0 is always the Farmer (owner). Then specialists, then farmhands.
        var roles = new List<NamedNpcArchetype> { new FarmerArchetype() };

        var specialists = new List<NamedNpcArchetype>();
        if (_sheepPen  != null) specialists.Add(new ShepherdArchetype());
        if (_dairyShed != null) specialists.Add(new DairymaidArchetype());
        specialists.Add(new SwineherdArchetype());
        specialists.Add(new PoultryKeeperArchetype());

        // Shuffle the specialist order
        for (int i = specialists.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (specialists[i], specialists[j]) = (specialists[j], specialists[i]);
        }

        foreach (var s in specialists)
        {
            if (roles.Count >= count) break;
            roles.Add(s);
        }
        while (roles.Count < count)
            roles.Add(new FarmhandArchetype());

        return roles;
    }

    private NpcEntity SpawnNamed(Random rng, NamedNpcArchetype archetype, string nodeContext)
    {
        AffinityTable? saved = null;
        if (_locationState?.NpcAffinityData.TryGetValue(archetype.ArchetypeId, out var dict) == true)
            saved = new AffinityTable(dict);
        else if (_locationState != null)
        {
            var newDict = new Dictionary<string, AffinityLevel>();
            _locationState.NpcAffinityData[archetype.ArchetypeId] = newDict;
            saved = new AffinityTable(newDict);
        }
        return archetype.Spawn(rng, nodeContext, saved);
    }

    private static void SpawnShallow(Random rng, Scene scene, ShallowNpcArchetype archetype, Area home, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var entity = archetype.Spawn(rng, home.DisplayName.ToLowerInvariant());
            var sceneNpc = new SceneNpc(entity);
            sceneNpc.Register(scene);
            scene.Npcs.Add(sceneNpc);
            scene.NpcSchedules[sceneNpc.Id] = NpcSchedule.Always(home.DisplayName.ToLowerInvariant());
        }
    }

    private NpcSchedule BuildScheduleForRole(string archetypeId, Area bedroom)
    {
        var bedroomId  = bedroom.DisplayName.ToLowerInvariant();
        var courtyard  = _courtyard!.DisplayName.ToLowerInvariant();
        var chickenId  = _chickenCoop!.DisplayName.ToLowerInvariant();
        var pigstyId   = _pigsty!.DisplayName.ToLowerInvariant();
        var gardenId   = _vegetableGarden!.DisplayName.ToLowerInvariant();
        var orchardId  = _orchard!.DisplayName.ToLowerInvariant();
        var shedId     = _shed!.DisplayName.ToLowerInvariant();
        var sheepId    = _sheepPen?.DisplayName.ToLowerInvariant();
        var dairyId    = _dairyShed?.DisplayName.ToLowerInvariant();
        var hallId     = "hall";
        var kitchenId  = "kitchen";

        return archetypeId switch
        {
            "farmer" => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = courtyard,
                [TimePeriod.Morning]   = gardenId,
                [TimePeriod.Noon]      = kitchenId,
                [TimePeriod.Afternoon] = orchardId,
                [TimePeriod.Evening]   = hallId,
                [TimePeriod.Night]     = bedroomId,
            }),

            "shepherd" when sheepId != null => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = sheepId,
                [TimePeriod.Morning]   = sheepId,
                [TimePeriod.Noon]      = courtyard,
                [TimePeriod.Afternoon] = sheepId,
                [TimePeriod.Evening]   = hallId,
                [TimePeriod.Night]     = bedroomId,
            }),

            "dairymaid" when dairyId != null => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = dairyId,
                [TimePeriod.Morning]   = dairyId,
                [TimePeriod.Noon]      = kitchenId,
                [TimePeriod.Afternoon] = dairyId,
                [TimePeriod.Evening]   = hallId,
                [TimePeriod.Night]     = bedroomId,
            }),

            "swineherd" => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = pigstyId,
                [TimePeriod.Morning]   = courtyard,
                [TimePeriod.Noon]      = courtyard,
                [TimePeriod.Afternoon] = pigstyId,
                [TimePeriod.Evening]   = hallId,
                [TimePeriod.Night]     = bedroomId,
            }),

            "poultry_keeper" => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = chickenId,
                [TimePeriod.Morning]   = orchardId,
                [TimePeriod.Noon]      = hallId,
                [TimePeriod.Afternoon] = chickenId,
                [TimePeriod.Evening]   = hallId,
                [TimePeriod.Night]     = bedroomId,
            }),

            _ /* farmhand */ => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = courtyard,
                [TimePeriod.Morning]   = shedId,
                [TimePeriod.Noon]      = hallId,
                [TimePeriod.Afternoon] = gardenId,
                [TimePeriod.Evening]   = hallId,
                [TimePeriod.Night]     = bedroomId,
            }),
        };
    }

    // ── Outdoor area builders ────────────────────────────────────────────────

    private static Area BuildCourtyard()
    {
        var area = new Area(
            displayName: "Courtyard",
            contextDescription: "standing in the farmyard courtyard",
            transitionDescription: "enter the courtyard",
            descriptions: new() { "The muddy central yard of the farm, hemmed by low fences and outbuildings" },
            moods: new[] { "muddy", "busy", "noisy", "open", "cluttered" }
        );
        return area;
    }

    private static Area BuildVegetableGarden(Random rng)
    {
        var garden = new Area(
            displayName: "Vegetable Garden",
            contextDescription: "in the kitchen garden",
            transitionDescription: "walk into the vegetable garden",
            descriptions: new() { "Rows of root vegetables and cabbages, stakes and string keeping order" },
            moods: new[] { "ordered", "earthy", "green", "damp", "productive" }
        );

        // 2–3 vegetable beds drawn from the spec list (always at least one root)
        var rootBeds = new List<Func<PointOfInterest>>
        {
            BuildTurnipBed, BuildCarrotBed, BuildRadishBed, BuildBeetrootBed, BuildParsnipBed,
        };
        var otherBeds = new List<Func<PointOfInterest>>
        {
            BuildOnionBed, BuildLeekBed, BuildCabbageBed, BuildPeaBed,
        };

        // One guaranteed root bed
        garden.PointsOfInterest.Add(rootBeds[rng.Next(rootBeds.Count)]());

        // 1–2 more beds
        int extraCount = rng.Next(1, 3);
        var rest = new List<Func<PointOfInterest>>(rootBeds);
        rest.AddRange(otherBeds);
        var extra = SampleUniqueIndices(rng, rest.Count, extraCount);
        foreach (var idx in extra)
            garden.PointsOfInterest.Add(rest[idx]());

        return garden;
    }

    private static Area BuildOrchard(Random rng)
    {
        var orchard = new Area(
            displayName: "Orchard",
            contextDescription: "under the orchard trees",
            transitionDescription: "walk into the orchard",
            descriptions: new() { "A half-dozen gnarled fruit trees, their branches tangled overhead" },
            moods: new[] { "gnarled", "shaded", "sweet", "overgrown", "old", "mossy" }
        );

        // 1–2 fruit-tree species
        int treeKinds = rng.Next(1, 3);
        var tools = new List<Func<PointOfInterest>>
        {
            TerrainSubfactory.BuildAppleTree, TerrainSubfactory.BuildPearTree,
            TerrainSubfactory.BuildPlumTree,  TerrainSubfactory.BuildCherryTree,
        };
        var picks = SampleUniqueIndices(rng, tools.Count, treeKinds);
        foreach (var idx in picks)
        {
            orchard.PointsOfInterest.Add(tools[idx]());
            orchard.PointsOfInterest.Add(tools[idx]());
        }
        return orchard;
    }

    private static Area BuildStorageShed()
    {
        var shed = new Area(
            displayName: "Storage Shed",
            contextDescription: "inside the storage shed",
            transitionDescription: "step into the storage shed",
            descriptions: new() { "A low-roofed storage shed smelling of hay, rust, and old wood" },
            moods: new[] { "low", "dusty", "dry", "cluttered", "dim", "rusty" }
        );

        shed.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Hay Stack",
            descriptions: new() { "A compressed stack of hay rising to the shed roof" },
            items: new()
            {
                new ItemElement(new Hay()),
                new ItemElement(new Hay()),
                new ItemElement(new Straw()),
            },
            moods: new[] { "tall", "dry", "sweet-smelling", "golden" }
        ));

        shed.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Grain Sacks",
            descriptions: new() { "Cloth sacks of dried grain stacked along the wall" },
            items: new()
            {
                new ItemElement(new Grain()),
                new ItemElement(new Grain()),
            },
            moods: new[] { "heavy", "dim", "dusty", "full" }
        ));

        shed.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Tool Rack",
            descriptions: new() { "A wooden rack of farm tools hanging from iron pegs" },
            items: new()
            {
                new ItemElement(new Sickle()),
                new ItemElement(new Hatchet()),
                new ItemElement(new Rope()),
            },
            moods: new[] { "cluttered", "dim", "rusty", "functional" }
        ));

        shed.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Barrel",
            descriptions: new() { "A heavy oak barrel, iron-banded and full" },
            items: new()
            {
                new ItemElement(new Ale()),
            },
            moods: new[] { "heavy", "iron-banded", "dim" }
        ));

        return shed;
    }

    // ── Vegetable-bed PoI builders ───────────────────────────────────────────

    private static PointOfInterest BuildTurnipBed() => new(
        displayName: "Turnip Bed",
        descriptions: new() { "A row of swollen turnips half-emerged from the dark earth" },
        items: new() { new ItemElement(new Turnip()), new ItemElement(new Turnip()) },
        moods: new[] { "earthy", "neat", "green", "damp" }
    );

    private static PointOfInterest BuildCarrotBed() => new(
        displayName: "Carrot Bed",
        descriptions: new() { "A bed of carrots, feathery tops waving above the soil" },
        items: new() { new ItemElement(new Carrot()), new ItemElement(new Carrot()) },
        moods: new[] { "bright", "feathery", "neat", "damp" }
    );

    private static PointOfInterest BuildRadishBed() => new(
        displayName: "Radish Bed",
        descriptions: new() { "A bed of fat-skinned radishes pushing through the soil" },
        items: new() { new ItemElement(new Radish()), new ItemElement(new Radish()) },
        moods: new[] { "neat", "earthy", "red-topped" }
    );

    private static PointOfInterest BuildBeetrootBed() => new(
        displayName: "Beetroot Bed",
        descriptions: new() { "A row of beetroots, leaves dark with a wine-stained edge" },
        items: new() { new ItemElement(new Beetroot()), new ItemElement(new Beetroot()) },
        moods: new[] { "ordered", "dark-leaved", "earthy" }
    );

    private static PointOfInterest BuildParsnipBed() => new(
        displayName: "Parsnip Bed",
        descriptions: new() { "A row of parsnips, pale tops half-buried in damp soil" },
        items: new() { new ItemElement(new Parsnip()), new ItemElement(new Parsnip()) },
        moods: new[] { "ordered", "pale", "earthy" }
    );

    private static PointOfInterest BuildOnionBed() => new(
        displayName: "Onion Bed",
        descriptions: new() { "A row of onions, papery tops yellowing as they ripen" },
        items: new() { new ItemElement(new Onion()), new ItemElement(new Onion()) },
        moods: new[] { "papery", "ordered", "yellowing" }
    );

    private static PointOfInterest BuildLeekBed() => new(
        displayName: "Leek Bed",
        descriptions: new() { "A row of leeks, dark-green leaves rising in tidy ranks" },
        items: new() { new ItemElement(new Leek()), new ItemElement(new Leek()) },
        moods: new[] { "ordered", "tall", "dark-leaved" }
    );

    private static PointOfInterest BuildCabbageBed() => new(
        displayName: "Cabbage Bed",
        descriptions: new() { "A row of round cabbages, leaves curling tightly around their cores" },
        items: new() { new ItemElement(new Cabbage()), new ItemElement(new Cabbage()) },
        moods: new[] { "rounded", "ordered", "green" }
    );

    private static PointOfInterest BuildPeaBed() => new(
        displayName: "Pea Bed",
        descriptions: new() { "A row of pea-vines climbing wooden stakes, pods hanging plump" },
        items: new() { new ItemElement(new Pea()), new ItemElement(new Pea()) },
        moods: new[] { "climbing", "tangled", "green", "fresh" }
    );

    // ── Main entrance door ──────────────────────────────────────────────────

    private static DoorPointOfInterest BuildMainEntranceDoor(Area courtyard, Area hall, BuildingMaterial material)
    {
        var matWord = material switch
        {
            BuildingMaterial.Stone        => "heavy oak",
            BuildingMaterial.WattleAndDaub => "low timber",
            BuildingMaterial.Timber       => "oak-planked",
            _                             => "wooden",
        };

        return new DoorPointOfInterest(
            frontArea:   courtyard,
            backArea:    hall,
            displayName: "Farmhouse Door",
            descriptions: new() { $"A {matWord} door set into the front wall of the farmhouse, iron-banded and weathered" },
            initialState: DoorState.Locked
        );
    }
}
