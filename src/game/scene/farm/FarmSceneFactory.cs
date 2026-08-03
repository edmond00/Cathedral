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
using Cathedral.Fight.Generators;

namespace Cathedral.Game.Scene.Farm;

/// <summary>
/// Builds a complete medieval farm scene per the v1 world-content spec (farm.md).
///
/// Sections:
///   • Farmyard       — Courtyard (hub), Chicken Coop, Pigsty, Sheep Pen, Storage Shed, Dairy Shed
///   • Farm Grounds   — Vegetable Garden, Orchard
///   • Longhouse      — the household's building: public hall, kitchen, dormitories
///   • Barracks       — optional second building when the crew outgrows the longhouse
///
/// Connections: <see cref="PathPointOfInterest"/> (Farmyard Tracks, Garden Paths) between
/// Courtyard and outdoor areas; buildings are entered through their own doors.
///
/// <para><b>The roster decides the buildings, not the reverse.</b> Farm hands used to be dealt into
/// however many bedrooms the house happened to roll — which capped a whole farm at three people and
/// meant <c>FarmhandArchetype</c> could never spawn at all, since the specialists always filled the
/// slots first. Now the crew is drawn up first and the buildings are sized to sleep it.</para>
///
/// NPCs: Farmer, Shepherd, Dairymaid, Swineherd, Poultry Keeper, 0–3 Farmhands.
/// Shallow: Sheep, Pig, Chicken, Cow.
/// </summary>
public class FarmSceneFactory : SceneFactory
{
    public FarmSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private BuildingResult? _longhouse, _barracks;
    private LayoutShape _layout;
    private List<NamedNpcArchetype> _roster = new();
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
            new() { "The central yard of the farm, mud-churned and busy with animal sounds" },
            seed => new GeometricGenerator { Seed = seed }
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
            new() { "The working land around the farmhouse: gardens, orchards, and enclosures" },
            seed => new NoisyGenerator { Seed = seed, Density = 0.85f }
        );
        grounds.Areas.Add(_vegetableGarden);
        grounds.Areas.Add(_orchard);
        scene.Sections.Add(grounds);
        RegisterAll(scene, grounds);

        // ── 4. Connect outdoor areas ─────────────────────────────────────────
        // The shape is rolled rather than always a hub off the courtyard: a chain farm strings its
        // pens out along one track, a ring lets you come back round the far side. The courtyard leads
        // the list either way, so it stays the arrival point and the buildings' front yard.

        var outdoorAreas = new List<Area> { _courtyard, _chickenCoop, _pigsty, _shed, _vegetableGarden, _orchard };
        if (_sheepPen  != null) outdoorAreas.Add(_sheepPen);
        if (_dairyShed != null) outdoorAreas.Add(_dairyShed);

        _layout = OutdoorLayout.RollShape(rng);
        OutdoorLayout.Connect(scene, outdoorAreas, _layout, "Track", rng);

        // ── 5. Draw up the crew, then build to fit it ────────────────────────

        _roster = BuildRoster(rng);

        // The longhouse sleeps up to five; anything above that gets a barracks alongside. The farmer
        // always sleeps under the longhouse roof — it is their hall and their business room.
        int longhouseBeds = Math.Min(_roster.Count, 5);
        int barracksBeds  = _roster.Count - longhouseBeds;

        _longhouse = BuildingFactory.Build(new BuildingSpec
        {
            BuildingName          = "Longhouse",
            RoomPrefix            = "Longhouse",
            Access                = BuildingAccess.Public,
            Occupancy             = BuildingOccupancy.Communal,
            OutsideArea           = _courtyard,
            BedCount              = longhouseBeds,
            FunctionNoun          = "farmhouse",
            ArenaGeneratorFactory = seed => new RoomsGenerator { Seed = seed },
        }, rng);
        RegisterBuilding(scene, _longhouse);

        if (barracksBeds > 0)
        {
            _barracks = BuildingFactory.Build(new BuildingSpec
            {
                BuildingName = "Farm Barracks",
                RoomPrefix   = "Barracks",
                Access       = BuildingAccess.Private,
                Occupancy    = BuildingOccupancy.Communal,
                OutsideArea  = OutdoorLayout.DistributeEntrances(outdoorAreas, 1, rng)[0],
                BedCount     = barracksBeds,
                FunctionNoun = "bunkhouse",
            }, rng);
            RegisterBuilding(scene, _barracks);
        }

        Console.WriteLine($"FarmSceneFactory: Built farm — layout={_layout}, sheep={_hasSheep} dairy={_hasDairy}, "
                        + $"{_roster.Count} worker(s), {longhouseBeds} longhouse bed(s), {barracksBeds} barracks bed(s)");
    
        // ── Furnishing: somewhere to sit, somewhere to hide, a hard shortcut, a climb ──
        // Rolled, so two places of the same kind are not the same place. Runs after the sections and
        // paths exist: shortcuts need to know what is already adjacent, and the climb needs a section
        // to put its top area in.
        {
            var outdoors = scene.OutdoorAreas;
            FurnitureSubfactory.AddSitSpots(rng, outdoors, FurnitureSubfactory.Setting.Farmland);
            FurnitureSubfactory.AddHidingPlaces(rng, outdoors, FurnitureSubfactory.Setting.Farmland);
            FurnitureSubfactory.AddShortcuts(rng, scene, outdoors, FurnitureSubfactory.Setting.Farmland);
            FurnitureSubfactory.AddExtractionPoints(rng, outdoors, FurnitureSubfactory.Setting.Farmland);

            var climbTop = FurnitureSubfactory.AddClimb(
                rng, scene, outdoors[0], FurnitureSubfactory.Setting.Farmland);
            if (climbTop != null)
            {
                // Sections must partition the areas, so the new top belongs to the section its foot is
                // in — an area in no section crashes the fight path outright.
                var host = scene.Sections.First(s => s.Areas.Contains(outdoors[0]));
                host.Areas.Add(climbTop);
                RegisterAll(scene, climbTop);
            }
        }

        // Landmarks, and a view from anywhere that has to be climbed to. Must run after the
        // connectors are attached: it finds the high ground by looking for their tops.
        MarkLandmarksAndViews(scene);
}

    /// <summary>
    /// The farm's people, decided before a single wall goes up: the farmer, whichever specialists the
    /// rolled outbuildings call for, and 0–3 hands on top.
    /// </summary>
    private List<NamedNpcArchetype> BuildRoster(Random rng)
    {
        var roles = new List<NamedNpcArchetype> { new FarmerArchetype() };

        if (_sheepPen  != null) roles.Add(new ShepherdArchetype());
        if (_dairyShed != null) roles.Add(new DairymaidArchetype());
        roles.Add(new SwineherdArchetype());
        roles.Add(new PoultryKeeperArchetype());

        // Hands scale with the farm, and — unlike before — actually get dealt in: they are appended
        // to the roster rather than left to fill beds the specialists had already taken.
        int hands = rng.Next(0, 4);
        for (int i = 0; i < hands; i++)
            roles.Add(new FarmhandArchetype());

        return roles;
    }

    // ── NPC construction ────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_longhouse is null || _courtyard is null) return;

        // One bed per worker, longhouse first then barracks — the buildings were sized from this same
        // roster, so the two lists are the same length by construction.
        var beds = _longhouse.BedAreas
            .Concat(_barracks?.BedAreas ?? Array.Empty<Area>())
            .ToList();

        var hall      = _longhouse.PublicHall;
        var schedules = new List<NpcSchedule>();

        for (int i = 0; i < _roster.Count && i < beds.Count; i++)
        {
            var archetype = _roster[i];
            var bed       = beds[i];
            var entity    = SpawnNamed(rng, archetype, "a medieval farm");

            // The farmer holds the farm: both buildings are theirs, which makes them the authority
            // any witness or threat check defers to anywhere indoors.
            if (i == 0)
            {
                entity.OwnedSectionIds.Add(_longhouse.Section.Id.ToString());
                if (_barracks != null) entity.OwnedSectionIds.Add(_barracks.Section.Id.ToString());
            }

            var schedule = BuildScheduleForRole(archetype.ArchetypeId, bed, hall, rng);
            schedules.Add(schedule);

            var sceneNpc = new SceneNpc(entity);
            sceneNpc.Register(scene);
            scene.Npcs.Add(sceneNpc);
            scene.NpcSchedules[sceneNpc.Id] = schedule;

            Console.WriteLine($"FarmSceneFactory: Spawned {entity.DisplayName} ({archetype.ArchetypeId}) — sleeps in {bed.DisplayName}");
        }

        // Hands may be pulled in to the hall; the farmer never is. Unlike a village workshop, a farm
        // hall is allowed to stand empty for the one period its master is out — there is no counter
        // to mind, only a household — so no one is drafted to cover it.
        if (schedules.Count > 1)
            BuildingSchedule.StaffPublicHall(hall, schedules.Skip(1).ToList());

        // ── Shallow animals ─────────────────────────────────────────────────

        SpawnShallow(rng, scene, new ChickenArchetype(), _chickenCoop!, count: rng.Next(3, 7));
        SpawnShallow(rng, scene, new PigArchetype(),     _pigsty!,      count: rng.Next(1, 4));

        if (_sheepPen != null)
            SpawnShallow(rng, scene, new SheepArchetype(), _sheepPen, count: rng.Next(2, 7));
        if (_dairyShed != null)
            SpawnShallow(rng, scene, new CowArchetype(), _dairyShed, count: rng.Next(1, 3));
    
        // Small life. Every location has some; which and how many is rolled, so two
        // places of the same kind are not the same place.
        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Cultivated, 2, 5);
}

    private NpcEntity SpawnNamed(Random rng, NamedNpcArchetype archetype, string nodeContext)
    {
        // Affinity persists per NPC: Spawn resolves the table by the NPC's stable id.
        return archetype.Spawn(rng, nodeContext,
            _locationState != null ? _locationState.AffinityFor : null);
    }

    private static void SpawnShallow(Random rng, Scene scene, ShallowNpcArchetype archetype, Area home, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var entity = archetype.Spawn(rng, home.DisplayName.ToLowerInvariant());
            var sceneNpc = new SceneNpc(entity);
            sceneNpc.Register(scene);
            scene.Npcs.Add(sceneNpc);
            scene.NpcSchedules[sceneNpc.Id] = NpcSchedule.Always(home);
        }
    }

    /// <summary>
    /// A farm worker's day. Night is always their own bed — see <see cref="BuildingSchedule"/> — and
    /// the day is spent across the outbuildings their role actually works, with the longhouse hall as
    /// the place they turn up to eat and be found.
    /// </summary>
    private NpcSchedule BuildScheduleForRole(string archetypeId, Area bed, Area hall, Random rng)
    {
        var yard    = _courtyard!;
        var chicken = _chickenCoop!;
        var pigsty  = _pigsty!;
        var garden  = _vegetableGarden!;
        var orchard = _orchard!;
        var shed    = _shed!;

        return archetypeId switch
        {
            // The farmer is the one who does business here, so the hall is their workplace and they
            // step out for only one period of the day.
            "farmer" => BuildingSchedule.ForWorker(
                bed, hall, new[] { yard, garden, orchard }, rng, awayPeriods: 1),

            "shepherd" when _sheepPen != null =>
                BuildingSchedule.ForHand(bed, new[] { _sheepPen, _sheepPen, yard, hall }, rng),

            "dairymaid" when _dairyShed != null =>
                BuildingSchedule.ForHand(bed, new[] { _dairyShed, _dairyShed, hall, yard }, rng),

            "swineherd" =>
                BuildingSchedule.ForHand(bed, new[] { pigsty, pigsty, yard, hall }, rng),

            "poultry_keeper" =>
                BuildingSchedule.ForHand(bed, new[] { chicken, chicken, orchard, hall }, rng),

            _ /* farmhand and any specialist whose outbuilding did not spawn */ =>
                BuildingSchedule.ForHand(bed, new[] { yard, shed, garden, orchard, hall }, rng),
        };
    }

    // ── Outdoor area builders ────────────────────────────────────────────────

    private static Area BuildCourtyard()
    {
        var area = new Area(
            displayName: "Courtyard",
            referenceLemma: "courtyard",
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
            referenceLemma: "garden",
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
        int rootPick = rng.Next(rootBeds.Count);
        garden.PointsOfInterest.Add(rootBeds[rootPick]());

        // 1–2 more beds, drawn from everything except the root bed already planted — the guaranteed
        // pick used to stay in the pool, so a garden could end up with two "Turnip Bed" PoIs and only
        // one of them observable.
        var rest = new List<Func<PointOfInterest>>(rootBeds);
        rest.RemoveAt(rootPick);
        rest.AddRange(otherBeds);
        foreach (var idx in SampleUniqueIndices(rng, rest.Count, rng.Next(1, 3)))
            garden.PointsOfInterest.Add(rest[idx]());

        return garden;
    }

    private static Area BuildOrchard(Random rng)
    {
        var orchard = new Area(
            displayName: "Orchard",
            referenceLemma: "orchard",
            contextDescription: "under the orchard trees",
            transitionDescription: "walk into the orchard",
            descriptions: new() { "A half-dozen gnarled fruit trees, their branches tangled overhead" },
            moods: new[] { "gnarled", "shaded", "sweet", "overgrown", "old", "mossy" }
        );

        // 2–4 fruit-tree species, one stand each. This used to add each chosen species twice, which
        // gave the orchard two PoIs called "Apple Tree" — and observation candidates are de-duplicated
        // by name, so the second was never offered. Same PoI count, all of them reachable.
        var tools = new List<Func<PointOfInterest>>
        {
            TerrainSubfactory.BuildAppleTree, TerrainSubfactory.BuildPearTree,
            TerrainSubfactory.BuildPlumTree,  TerrainSubfactory.BuildCherryTree,
        };
        foreach (var idx in SampleUniqueIndices(rng, tools.Count, rng.Next(2, 5)))
            orchard.PointsOfInterest.Add(tools[idx]());

        return orchard;
    }

    private static Area BuildStorageShed()
    {
        var shed = new Area(
            displayName: "Storage Shed",
            referenceLemma: "shed",
            contextDescription: "inside the storage shed",
            transitionDescription: "step into the storage shed",
            descriptions: new() { "A low-roofed storage shed smelling of hay, rust, and old wood" },
            moods: new[] { "low", "dusty", "dry", "cluttered", "dim", "rusty" }
        );

        shed.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Hay Stack",
            referenceLemma: "hay",
            descriptions: new() { "A compressed stack of hay rising to the shed roof" },
            items: new()
            {
                new ItemElement(new Hay()),
                new ItemElement(new Hay()),
                new ItemElement(new Straw()),
            },
            moods: new[] { "tall", "dry", "sweet-smelling", "golden" }
        ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "harvestry", ["smell"] = "scenting" } });

        shed.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Grain Sacks",
            referenceLemma: "grain",
            descriptions: new() { "Cloth sacks of dried grain stacked along the wall" },
            items: new()
            {
                new ItemElement(new Grain()),
                new ItemElement(new Grain()),
            },
            moods: new[] { "heavy", "dim", "dusty", "full" }
        ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "harvestry", ["smell"] = "scenting" } });

        shed.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Tool Rack",
            referenceLemma: "tool",
            descriptions: new() { "A wooden rack of farm tools hanging from iron pegs" },
            items: new()
            {
                new ItemElement(new Sickle()),
                new ItemElement(new Hatchet()),
                new ItemElement(new Rope()),
            },
            moods: new[] { "cluttered", "dim", "rusty", "functional" }
        ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "metalcraft" } });

        shed.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Barrel",
            referenceLemma: "barrel",
            descriptions: new() { "A heavy oak barrel, iron-banded and full" },
            items: new()
            {
                new ItemElement(new Ale()),
            },
            moods: new[] { "heavy", "iron-banded", "dim" }
        ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "cellarcraft", ["smell"] = "scenting" } });

        return shed;
    }

    // ── Vegetable-bed PoI builders ───────────────────────────────────────────

    private static PointOfInterest BuildTurnipBed() => new(
        displayName: "Turnip Bed",
        referenceLemma: "turnip",
        descriptions: new() { "A row of swollen turnips half-emerged from the dark earth" },
        items: new() { new ItemElement(new Turnip()), new ItemElement(new Turnip()) },
        moods: new[] { "earthy", "neat", "green", "damp" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "seed_lore", ["smell"] = "scenting" } };

    private static PointOfInterest BuildCarrotBed() => new(
        displayName: "Carrot Bed",
        referenceLemma: "carrot",
        descriptions: new() { "A bed of carrots, feathery tops waving above the soil" },
        items: new() { new ItemElement(new Carrot()), new ItemElement(new Carrot()) },
        moods: new[] { "bright", "feathery", "neat", "damp" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "seed_lore", ["smell"] = "scenting" } };

    private static PointOfInterest BuildRadishBed() => new(
        displayName: "Radish Bed",
        referenceLemma: "radish",
        descriptions: new() { "A bed of fat-skinned radishes pushing through the soil" },
        items: new() { new ItemElement(new Radish()), new ItemElement(new Radish()) },
        moods: new[] { "neat", "earthy", "red-topped" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "seed_lore", ["smell"] = "scenting" } };

    private static PointOfInterest BuildBeetrootBed() => new(
        displayName: "Beetroot Bed",
        referenceLemma: "beetroot",
        descriptions: new() { "A row of beetroots, leaves dark with a wine-stained edge" },
        items: new() { new ItemElement(new Beetroot()), new ItemElement(new Beetroot()) },
        moods: new[] { "ordered", "dark-leaved", "earthy" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "seed_lore", ["smell"] = "scenting" } };

    private static PointOfInterest BuildParsnipBed() => new(
        displayName: "Parsnip Bed",
        referenceLemma: "parsnip",
        descriptions: new() { "A row of parsnips, pale tops half-buried in damp soil" },
        items: new() { new ItemElement(new Parsnip()), new ItemElement(new Parsnip()) },
        moods: new[] { "ordered", "pale", "earthy" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "seed_lore", ["smell"] = "scenting" } };

    private static PointOfInterest BuildOnionBed() => new(
        displayName: "Onion Bed",
        referenceLemma: "onion",
        descriptions: new() { "A row of onions, papery tops yellowing as they ripen" },
        items: new() { new ItemElement(new Onion()), new ItemElement(new Onion()) },
        moods: new[] { "papery", "ordered", "yellowing" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "seed_lore", ["smell"] = "scenting" } };

    private static PointOfInterest BuildLeekBed() => new(
        displayName: "Leek Bed",
        referenceLemma: "leek",
        descriptions: new() { "A row of leeks, dark-green leaves rising in tidy ranks" },
        items: new() { new ItemElement(new Leek()), new ItemElement(new Leek()) },
        moods: new[] { "ordered", "tall", "dark-leaved" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "seed_lore", ["smell"] = "scenting" } };

    private static PointOfInterest BuildCabbageBed() => new(
        displayName: "Cabbage Bed",
        referenceLemma: "cabbage",
        descriptions: new() { "A row of round cabbages, leaves curling tightly around their cores" },
        items: new() { new ItemElement(new Cabbage()), new ItemElement(new Cabbage()) },
        moods: new[] { "rounded", "ordered", "green" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "tillage", ["smell"] = "scenting" } };

    private static PointOfInterest BuildPeaBed() => new(
        displayName: "Pea Bed",
        referenceLemma: "pea",
        descriptions: new() { "A row of pea-vines climbing wooden stakes, pods hanging plump" },
        items: new() { new ItemElement(new Pea()), new ItemElement(new Pea()) },
        moods: new[] { "climbing", "tangled", "green", "fresh" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "tillage", ["smell"] = "scenting" } };

    // The hand-built farmhouse entrance door lived here. BuildingFactory now makes every entry door,
    // rolls its description against the building it opens into, and applies the night rule to it.
}
