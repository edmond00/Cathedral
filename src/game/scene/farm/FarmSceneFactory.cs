using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Farm;

/// <summary>
/// Builds a complete medieval farm scene.
///
/// Building (via <see cref="HouseBuilder"/>):
///   Ground floor — Hall, Kitchen, optional Pantry
///   Upper floor  — Upper Landing, 1-3 Bedrooms  (or ground-floor bedrooms if single-storey)
///   Inter-room movement via <see cref="DoorSpot"/>; floors via <see cref="StairSpot"/>
///   Main entrance: locked <see cref="DoorSpot"/> from Courtyard → Hall
///
/// Outside sections:
///   Farmyard  — Courtyard (hub), Chicken Coop, Pigsty
///   Grounds   — Vegetable Garden, Orchard, Rabbit Enclosure, Shed
///
/// Outdoor areas are connected bidirectionally via the area graph with Courtyard as the hub.
///
/// NPCs:
///   One NPC spawned per bedroom.
///   First NPC: <see cref="FarmerArchetype"/>.
///   Additional NPCs: <see cref="FarmhandArchetype"/>.
///   Each NPC receives a day-cycle schedule appropriate to their role.
/// </summary>
public class FarmSceneFactory : SceneFactory
{
    public FarmSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    // ── Section/area construction ─────────────────────────────────────────────

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // ── 1. Build outdoor areas first (Courtyard must be scene.AllAreas[0]) ─

        var courtyard   = BuildCourtyard();
        var chickenCoop = BuildChickenCoop();
        var pigsty      = BuildPigsty();

        var vegetableGarden = BuildVegetableGarden();
        var orchard         = BuildOrchard();
        var rabbitEnclosure = BuildRabbitEnclosure();
        var shed            = BuildShed();

        // Populate outdoor spots before registration
        PopulateChickenCoopPointsOfInterest(chickenCoop);
        PopulateShedPointsOfInterest(shed);
        PopulateVegetableGardenPointsOfInterest(vegetableGarden, rng);
        PopulateOrchardPointsOfInterest(orchard, rng);

        // Farmyard section — added to scene first so Courtyard is AllAreas[0]
        var farmyard = new Section(
            "Farmyard",
            new() { "The central yard of the farm, mud-churned and busy with animal sounds" }
        );
        farmyard.Areas.AddRange(new[] { courtyard, chickenCoop, pigsty });
        RegisterAll(scene, farmyard);
        scene.Sections.Add(farmyard);

        // Grounds section
        var grounds = new Section(
            "Farm Grounds",
            new() { "The working land around the farmhouse: gardens, orchards, and enclosures" }
        );
        grounds.Areas.AddRange(new[] { vegetableGarden, orchard, rabbitEnclosure, shed });
        RegisterAll(scene, grounds);
        scene.Sections.Add(grounds);

        // ── 2. Connect outdoor areas (Courtyard as hub) ───────────────────────

        var outdoorAreas = new[] { chickenCoop, pigsty, vegetableGarden, orchard, rabbitEnclosure, shed };
        foreach (var area in outdoorAreas)
            scene.ConnectAreasBidirectional(courtyard, area);

        scene.ConnectAreasBidirectional(vegetableGarden, orchard);

        // ── 3. Build the farmhouse ────────────────────────────────────────────

        var houseBuilder = new HouseBuilder
        {
            MinBedrooms = 1,
            MaxBedrooms = rng.Next(1, 4), // 1-3
            MaxFloors   = rng.NextDouble() < 0.65 ? 2 : 1,
        };

        var house = houseBuilder.Build(rng);
        HouseBuilder.PopulateFurniture(house, rng);

        foreach (var section in house.Sections)
        {
            // Mark all interior house areas as private — entering or stealing here is illegal.
            foreach (var area in section.Areas)
                area.IsPrivate = true;

            RegisterAll(scene, section);
            scene.Sections.Add(section);
        }

        Console.WriteLine($"FarmSceneFactory: Built farmhouse — {house.Bedrooms.Count} bedroom(s), {house.Sections.Count} section(s)");

        // ── 4. Main entrance door: Courtyard (front) → Hall (back) ───────────

        var entranceDoor = BuildMainEntranceDoor(courtyard, house.EntryRoom, house.Material);
        courtyard.PointsOfInterest.Add(entranceDoor);
        house.EntryRoom.PointsOfInterest.Add(entranceDoor);
        entranceDoor.Register(scene);

        Console.WriteLine($"FarmSceneFactory: Entry is Courtyard, entrance door placed");

        // Store house result for NPC phase
        _houseResult      = house;
        _courtyard        = courtyard;
        _chickenCoop      = chickenCoop;
        _pigsty           = pigsty;
        _rabbitEnclosure  = rabbitEnclosure;
        _garden           = vegetableGarden;
        _orchard          = orchard;
        _shed             = shed;
    }

    // Fields set during BuildSections for use in BuildNpcs
    private HouseResult? _houseResult;
    private Area? _courtyard, _chickenCoop, _pigsty, _rabbitEnclosure, _garden, _orchard, _shed;

    // ── NPC construction ──────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_houseResult is null || _courtyard is null) return;

        // ── Human NPCs (farmer + farmhands, one per bedroom) ─────────────────

        var bedrooms = _houseResult.Bedrooms;
        for (int i = 0; i < bedrooms.Count; i++)
        {
            var bedroom  = bedrooms[i];
            NamedNpcArchetype archetype = i == 0 ? new FarmerArchetype() : new FarmhandArchetype();

            // Restore saved affinity for this NPC archetype if available
            AffinityTable? savedAffinity = null;
            if (_locationState?.NpcAffinityData.TryGetValue(archetype.ArchetypeId, out var affinityDict) == true)
                savedAffinity = new AffinityTable(affinityDict);
            else if (_locationState != null)
            {
                var newDict = new Dictionary<string, AffinityLevel>();
                _locationState.NpcAffinityData[archetype.ArchetypeId] = newDict;
                savedAffinity = new AffinityTable(newDict);
            }

            var entity   = archetype.Spawn(rng, "a medieval farm", savedAffinity);

            // The farmer (index 0) owns all house sections.
            if (i == 0)
            {
                foreach (var section in _houseResult.Sections)
                    entity.OwnedSectionIds.Add(section.Id.ToString());
            }

            var sceneNpc = new SceneNpc(entity);
            sceneNpc.Register(scene);
            scene.Npcs.Add(sceneNpc);

            var schedule = BuildFarmSchedule(i, bedroom, _courtyard, _chickenCoop!, _pigsty!, _garden!, _orchard!, _shed!);
            scene.NpcSchedules[sceneNpc.Id] = schedule;

            Console.WriteLine($"FarmSceneFactory: Spawned {entity.DisplayName} ({archetype.ArchetypeId}), beds in '{bedroom.DisplayName}'");
        }

        // ── Shallow NPCs: farm animals ────────────────────────────────────────

        SpawnShallowNpcs(rng, scene, new ChickenArchetype(), _chickenCoop!,
            NpcSchedule.Always(_chickenCoop!.DisplayName.ToLowerInvariant()), count: rng.Next(3, 7));

        SpawnShallowNpcs(rng, scene, new RabbitArchetype(), _rabbitEnclosure!,
            NpcSchedule.Always(_rabbitEnclosure!.DisplayName.ToLowerInvariant()), count: rng.Next(3, 8));

        SpawnShallowNpcs(rng, scene, new PigArchetype(), _pigsty!,
            NpcSchedule.Always(_pigsty!.DisplayName.ToLowerInvariant()), count: rng.Next(1, 3));
    }

    private static void SpawnShallowNpcs(
        Random rng, Scene scene,
        ShallowNpcArchetype archetype, Area homeArea,
        NpcSchedule schedule, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var entity   = archetype.Spawn(rng, homeArea.DisplayName.ToLowerInvariant());
            var sceneNpc = new SceneNpc(entity);
            sceneNpc.Register(scene);
            scene.Npcs.Add(sceneNpc);
            scene.NpcSchedules[sceneNpc.Id] = schedule;
        }
        Console.WriteLine($"FarmSceneFactory: Spawned {count}x {archetype.TypeDisplayName} in '{homeArea.DisplayName}'");
    }

    /// <summary>
    /// Creates a day-cycle schedule for a farm NPC.
    /// <paramref name="npcIndex"/> 0 = farmer (owner), 1+ = farmhand.
    /// </summary>
    private static NpcSchedule BuildFarmSchedule(
        int npcIndex, Area bedroom,
        Area courtyard, Area chickenCoop, Area pigsty,
        Area garden, Area orchard, Area shed)
    {
        var bedroomId    = bedroom.DisplayName.ToLowerInvariant();
        var courtyardId  = courtyard.DisplayName.ToLowerInvariant();
        var chickenId    = chickenCoop.DisplayName.ToLowerInvariant();
        var pigstyId     = pigsty.DisplayName.ToLowerInvariant();
        var gardenId     = garden.DisplayName.ToLowerInvariant();
        var orchardId    = orchard.DisplayName.ToLowerInvariant();
        var shedId       = shed.DisplayName.ToLowerInvariant();
        var hallId       = "hall";
        var kitchenId    = "kitchen";

        if (npcIndex == 0)
        {
            // Farmer: oversees the whole farm; morning chores → field work → meal → evening rest
            return NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = courtyardId,   // checks the yard at first light
                [TimePeriod.Morning]   = gardenId,       // tends vegetable garden
                [TimePeriod.Noon]      = kitchenId,      // midday meal
                [TimePeriod.Afternoon] = orchardId,      // fruit trees, general inspection
                [TimePeriod.Evening]   = hallId,         // supper in the hall
                [TimePeriod.Night]     = bedroomId,      // sleep
            });
        }
        else if (npcIndex == 1)
        {
            // First farmhand: animal morning → general work → evening in hall
            return NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = chickenId,      // collect eggs, feed chickens
                [TimePeriod.Morning]   = pigstyId,       // feed pigs, muck out
                [TimePeriod.Noon]      = hallId,         // midday meal
                [TimePeriod.Afternoon] = gardenId,       // help in the garden
                [TimePeriod.Evening]   = hallId,         // evening rest
                [TimePeriod.Night]     = bedroomId,
            });
        }
        else
        {
            // Additional farmhands: shed + yard work
            return NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = courtyardId,
                [TimePeriod.Morning]   = shedId,
                [TimePeriod.Noon]      = hallId,
                [TimePeriod.Afternoon] = orchardId,
                [TimePeriod.Evening]   = hallId,
                [TimePeriod.Night]     = bedroomId,
            });
        }
    }

    // ── Outdoor area builders ─────────────────────────────────────────────────

    private static Area BuildCourtyard() => new(
        displayName: "Courtyard",
        contextDescription: "standing in the farmyard courtyard",
        transitionDescription: "enter the courtyard",
        descriptions: new() { "The muddy central yard of the farm, hemmed by low fences and outbuildings" },
        moods: new[] { "muddy", "busy", "noisy", "open", "smelly", "functional", "cluttered" }
    );

    private static Area BuildChickenCoop() => new(
        displayName: "Chicken Coop",
        contextDescription: "inside the chicken coop",
        transitionDescription: "step into the chicken coop",
        descriptions: new() { "A low timber coop crowded with hens, smelling of damp feathers and droppings" },
        moods: new[] { "low", "smelly", "dim", "crowded", "warm", "clucking", "feathery" }
    );

    private static Area BuildPigsty() => new(
        displayName: "Pigsty",
        contextDescription: "by the pigsty",
        transitionDescription: "approach the pigsty",
        descriptions: new() { "A walled mud pen containing a sow and her piglets, grunting and rooting" },
        moods: new[] { "muddy", "smelly", "wet", "loud", "enclosed", "rank", "grunting" }
    );

    private static Area BuildVegetableGarden() => new(
        displayName: "Vegetable Garden",
        contextDescription: "in the kitchen garden",
        transitionDescription: "walk into the vegetable garden",
        descriptions: new() { "Rows of root vegetables and cabbages, stakes and string keeping order" },
        moods: new[] { "ordered", "earthy", "green", "quiet", "neat", "damp", "productive" }
    );

    private static Area BuildOrchard() => new(
        displayName: "Orchard",
        contextDescription: "under the orchard trees",
        transitionDescription: "walk into the orchard",
        descriptions: new() { "A half-dozen gnarled apple and pear trees, their branches tangled overhead" },
        moods: new[] { "gnarled", "shaded", "quiet", "sweet", "overgrown", "old", "mossy" }
    );

    private static Area BuildRabbitEnclosure() => new(
        displayName: "Rabbit Enclosure",
        contextDescription: "beside the rabbit enclosure",
        transitionDescription: "approach the rabbit enclosure",
        descriptions: new() { "A fenced hutch of timber and wire holding a dozen grey rabbits" },
        moods: new[] { "quiet", "small", "close", "soft", "earthy", "still" }
    );

    private static Area BuildShed() => new(
        displayName: "Shed",
        contextDescription: "inside the farm shed",
        transitionDescription: "step into the shed",
        descriptions: new() { "A low-roofed storage shed smelling of hay, rust, and old wood" },
        moods: new[] { "low", "dusty", "dry", "cluttered", "dim", "quiet", "rusty" }
    );

    // ── Outdoor point of interest population ─────────────────────────────────

    private static void PopulateChickenCoopPointsOfInterest(Area chickenCoop)
    {
        var nestBox = new PointOfInterest(
            displayName: "Nest Box",
            descriptions: new() { "A row of straw-lined boxes where hens lay" },
            items: new()
            {
                new ItemElement(new Egg()),
                new ItemElement(new Egg()),
                new ItemElement(new Straw()),
            },
            moods: new[] { "warm", "straw-lined", "dim", "fragrant" }
        );
        chickenCoop.PointsOfInterest.Add(nestBox);
    }

    private static void PopulateShedPointsOfInterest(Area shed)
    {
        var hayStack = new PointOfInterest(
            displayName: "Hay Stack",
            descriptions: new() { "A compressed stack of hay bales rising to the shed roof" },
            items: new()
            {
                new ItemElement(new Hay()),
                new ItemElement(new Hay()),
                new ItemElement(new Straw()),
            },
            moods: new[] { "tall", "dry", "sweet-smelling", "golden" }
        );

        var grainStore = new PointOfInterest(
            displayName: "Grain Sacks",
            descriptions: new() { "Cloth sacks of dried grain stacked along the wall" },
            items: new()
            {
                new ItemElement(new Grain()),
                new ItemElement(new Grain()),
            },
            moods: new[] { "heavy", "dim", "dusty", "full" }
        );

        var toolRack = new PointOfInterest(
            displayName: "Tool Rack",
            descriptions: new() { "A wooden rack of farm tools hanging from iron pegs on the wall" },
            items: new()
            {
                new ItemElement(new Sickle()),
                new ItemElement(new Hatchet()),
                new ItemElement(new Rope()),
            },
            moods: new[] { "cluttered", "dim", "rusty", "functional" }
        );

        shed.PointsOfInterest.Add(hayStack);
        shed.PointsOfInterest.Add(grainStore);
        shed.PointsOfInterest.Add(toolRack);
    }

    private static void PopulateVegetableGardenPointsOfInterest(Area garden, Random rng)
    {
        var turnipBed = new PointOfInterest(
            displayName: "Turnip Bed",
            descriptions: new() { "A row of swollen turnips half-emerged from the dark earth" },
            items: new()
            {
                new ItemElement(new Turnip()),
                new ItemElement(new Turnip()),
            },
            moods: new[] { "earthy", "neat", "green", "damp" }
        );

        var carrotBed = new PointOfInterest(
            displayName: "Carrot Bed",
            descriptions: new() { "A bed of carrots, feathery tops waving above the soil" },
            items: new()
            {
                new ItemElement(new Carrot()),
                new ItemElement(new Carrot()),
            },
            moods: new[] { "bright", "feathery", "neat", "damp" }
        );

        garden.PointsOfInterest.Add(turnipBed);
        garden.PointsOfInterest.Add(carrotBed);
    }

    private static void PopulateOrchardPointsOfInterest(Area orchard, Random rng)
    {
        var appleTree = new PointOfInterest(
            displayName: "Apple Tree",
            descriptions: new() { "A gnarled old apple tree, its boughs heavy with fruit" },
            items: new()
            {
                new ItemElement(new Apple()),
                new ItemElement(new Apple()),
                new ItemElement(new Branch()),
            },
            moods: new[] { "gnarled", "laden", "shaded", "sweet" }
        );

        orchard.PointsOfInterest.Add(appleTree);
    }

    // ── Main entrance door ────────────────────────────────────────────────────

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
