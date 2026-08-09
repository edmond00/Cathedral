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

namespace Cathedral.Game.Scene.Field;

/// <summary>
/// Builds a procedural cultivated-field scene per the v1 world-content spec (field.md).
///
/// Sections: Tilled Strips, Field Margin, plus a Longhouse and optionally a Barracks.
/// Areas (5 picked): Grain Strip (one of wheat/barley/rye, or flax in 10% of fields),
/// Vegetable Beds, optional Herb Patch (30%), Fallow Ground (50%), Irrigation Ditch.
/// Connections: <see cref="PathPointOfInterest"/> field tracks.
///
/// <para>NPCs: Reeve, Plowman×1–2, Reaper×1–3, Hayward, Bondman×1–3 — and they all sleep here. The
/// schedules used to set <c>[Night] = null</c> with a note that the workers "return to farm/village
/// at night", but locations are separate scenes: nobody arrived anywhere, they simply stopped
/// existing after dark. The field now has a longhouse of its own, and a barracks when the crew is
/// too large for it.</para>
/// </summary>
public class FieldSceneFactory : SceneFactory
{
    public FieldSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum GrainCrop { Wheat, Barley, Rye, Flax }

    private GrainCrop _crop;
    private Area? _grainStrip, _vegBeds, _herbPatch, _fallow, _ditch;
    private BuildingResult? _longhouse, _barracks;
    private LayoutShape _layout;
    private List<NamedNpcArchetype> _roster = new();
    private readonly List<Area> _allAreas = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // ── 1. Roll crop & area presence ────────────────────────────────────

        _crop = rng.NextDouble() < 0.10
            ? GrainCrop.Flax
            : (GrainCrop)rng.Next(0, 3); // wheat, barley, rye

        bool hasHerb   = rng.NextDouble() < 0.30;
        bool hasFallow = rng.NextDouble() < 0.50;

        // ── 2. Build areas (PoIs populated before registration) ─────────────

        _grainStrip = BuildGrainStrip(rng);
        _vegBeds    = BuildVegetableBeds(rng);
        _ditch      = BuildIrrigationDitch();
        if (hasHerb)   _herbPatch = BuildHerbPatch(rng);
        if (hasFallow) _fallow    = BuildFallowGround();

        var tilled = new Section(
            "Tilled Strips",
            new() { "Cultivated rows of crop running long across the worked ground" },
            seed => new NoisyGenerator { Seed = seed, Density = 0.88f }
        );
        tilled.Areas.Add(_grainStrip);
        tilled.Areas.Add(_vegBeds);
        if (_herbPatch != null) tilled.Areas.Add(_herbPatch);
        scene.Sections.Add(tilled);
        RegisterAll(scene, tilled);

        var margin = new Section(
            "Field Margin",
            new() { "The boundary strip between cultivation and wild ground" },
            seed => new NoisyGenerator { Seed = seed, Density = 0.82f }
        );
        margin.Areas.Add(_ditch);
        if (_fallow != null) margin.Areas.Add(_fallow);
        scene.Sections.Add(margin);
        RegisterAll(scene, margin);

        _allAreas.AddRange(tilled.Areas);
        _allAreas.AddRange(margin.Areas);

        // ── 3. Connect with PathPoIs ─────────────────────────────────────────

        // Shape is rolled: a chain field runs strip to strip along one headland, a ring loops back
        // round the ditch. The grain strip leads the list, so it stays the arrival point.
        _layout = OutdoorLayout.RollShape(rng);
        OutdoorLayout.Connect(scene, _allAreas, _layout, "Field Track", rng);

        // ── 4. Draw up the crew, then house it ──────────────────────────────

        _roster = BuildRoster(rng);

        int longhouseBeds = Math.Min(_roster.Count, 5);
        int barracksBeds  = _roster.Count - longhouseBeds;

        _longhouse = BuildingFactory.Build(new BuildingSpec
        {
            BuildingName          = "Field Longhouse",
            RoomPrefix            = "Longhouse",
            Access                = BuildingAccess.Public,
            Occupancy             = BuildingOccupancy.Communal,
            OutsideArea           = _grainStrip,
            BedCount              = longhouseBeds,
            FunctionNoun          = "longhouse",
            ArenaGeneratorFactory = seed => new RoomsGenerator { Seed = seed },
        }, rng);
        RegisterBuilding(scene, _longhouse);

        if (barracksBeds > 0)
        {
            _barracks = BuildingFactory.Build(new BuildingSpec
            {
                BuildingName = "Field Barracks",
                RoomPrefix   = "Barracks",
                Access       = BuildingAccess.Private,
                Occupancy    = BuildingOccupancy.Communal,
                OutsideArea  = OutdoorLayout.DistributeEntrances(_allAreas, 1, rng)[0],
                BedCount     = barracksBeds,
                FunctionNoun = "bunkhouse",
            }, rng);
            RegisterBuilding(scene, _barracks);
        }

        Console.WriteLine($"FieldSceneFactory: Built field — layout={_layout}, crop={_crop}, "
                        + $"areas={_allAreas.Count}, {_roster.Count} worker(s)");
    
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

        }

        // Every building has a roof, and from a roof you can see the rest of the outside.
        AddRoofLandscapes(scene);
        // something in it happened to be the top of a ladder or a rock step, and then a bare area
        // with a view over nothing worth naming. A view is now a deliberate statement about a
        // location, and this location does not make one.
}

    /// <summary>The field's crew, drawn up before the buildings so they can be sized to sleep it.</summary>
    private static List<NamedNpcArchetype> BuildRoster(Random rng)
    {
        var roles = new List<NamedNpcArchetype> { new ReeveArchetype() };

        for (int i = 0, n = rng.Next(1, 3); i < n; i++) roles.Add(new PlowmanArchetype());
        for (int i = 0, n = rng.Next(1, 4); i < n; i++) roles.Add(new ReaperArchetype());
        roles.Add(new HaywardArchetype());
        for (int i = 0, n = rng.Next(1, 4); i < n; i++) roles.Add(new BondmanArchetype());

        return roles;
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private Area BuildGrainStrip(Random rng)
    {
        var name = _crop switch
        {
            GrainCrop.Wheat  => "Wheat Strip",
            GrainCrop.Barley => "Barley Strip",
            GrainCrop.Rye    => "Rye Strip",
            GrainCrop.Flax   => "Flax Strip",
            _                => "Grain Strip",
        };
        var desc = _crop switch
        {
            GrainCrop.Flax  => "A long strip of pale flax stems standing in tidy ranks",
            _               => $"A long strip of {_crop.ToString().ToLowerInvariant()} running off into the haze, ears nodding",
        };

        var area = new Area(
            displayName: name,
            referenceLemma: "crop",
            contextDescription: $"walking the {name.ToLowerInvariant()}",
            transitionDescription: $"step onto the {name.ToLowerInvariant()}",
            descriptions: new() { desc },
            moods: new[] { "long", "ordered", "rustling", "exposed", "sun-warmed" }
        );

        // Crop Row (Grain Bundle, Straw) or Flax bundle
        var cropItems = _crop == GrainCrop.Flax
            ? new List<ItemElement> { new ItemElement(new Flax()), new ItemElement(new Flax()) }
            : new List<ItemElement> { new ItemElement(new Grain()), new ItemElement(new Grain()), new ItemElement(new Straw()) };
        area.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Crop Row",
            referenceLemma: "crop",
            descriptions: new() { _crop == GrainCrop.Flax ? "A row of pale flax stems, ready for harvest" : "A row of grain stems, ears heavy" },
            items: cropItems,
            moods: new[] { "long", "rustling", "ripe" }
        ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "harvestry", ["listen"] = "keen_ear", ["smell"] = "scenting" } });

        // Scarecrow
        area.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Scarecrow",
            referenceLemma: "scarecrow",
            descriptions: new() { "A straw-stuffed figure on a pole, hat slumped, sleeves blowing" },
            items: new()
            {
                new ItemElement(new Straw()),
                new ItemElement(new Rope()),
            },
            moods: new[] { "tattered", "lonely", "still", "wind-blown" }
        ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "peasantry", ["contemplate"] = "aesthetic" } });

        // Tool Rest with sickle and rake
        area.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Tool Rest",
            referenceLemma: "tool",
            descriptions: new() { "A wooden bench at the field-edge with tools leaning against it" },
            items: new()
            {
                new ItemElement(new Sickle()),
                new ItemElement(new Rake()),
            },
            moods: new[] { "low", "worn", "dusty" }
        ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "tillage" } });

        return area;
    }

    private static Area BuildVegetableBeds(Random rng)
    {
        var area = new Area(
            displayName: "Vegetable Beds",
        referenceLemma: "vegetable",
            contextDescription: "in the field's vegetable beds",
            transitionDescription: "step into the vegetable beds",
            descriptions: new() { "A cluster of raised beds, each row planted with a different crop" },
            moods: new[] { "tidy", "earthy", "green", "ordered" }
        );

        // Pick 2–3 distinct vegetables
        var vegBuilders = new List<Func<PointOfInterest>>
        {
            BuildTurnipMound, BuildRadishMound, BuildParsnipMound, BuildOnionMound,
            BuildLeekMound,   BuildCabbageMound, BuildPeaMound,    BuildBeetrootMound,
        };
        int count = rng.Next(2, 4);
        var picks = SampleUniqueIndices(rng, vegBuilders.Count, count);
        foreach (var idx in picks)
            area.PointsOfInterest.Add(vegBuilders[idx]());

        return area;
    }

    private static Area BuildHerbPatch(Random rng)
    {
        var area = new Area(
            displayName: "Herb Patch",
        referenceLemma: "herb",
            contextDescription: "in the herb patch",
            transitionDescription: "step into the herb patch",
            descriptions: new() { "A small fragrant patch of cultivated herbs at the field's quieter end" },
            moods: new[] { "fragrant", "small", "tidy", "green" }
        );

        var herbBuilders = new List<Func<PointOfInterest>>
        {
            BuildThymeClump, BuildSageClump, BuildMintClump, BuildChamomileClump, BuildWormwoodClump,
        };
        int count = rng.Next(2, 4);
        var picks = SampleUniqueIndices(rng, herbBuilders.Count, count);
        foreach (var idx in picks)
            area.PointsOfInterest.Add(herbBuilders[idx]());

        return area;
    }

    private static Area BuildFallowGround()
    {
        var area = new Area(
            displayName: "Fallow Ground",
        referenceLemma: "ground",
            contextDescription: "on the fallow ground",
            transitionDescription: "walk onto the fallow ground",
            descriptions: new() { "A resting strip of unworked land, weeds and grass reclaiming the furrows" },
            moods: new[] { "weedy", "still", "loose-soiled", "quiet" }
        );

        area.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Stone Marker",
            referenceLemma: "marker",
            descriptions: new() { "A flat stone set in the earth, marking a boundary" },
            items: new() { new ItemElement(new Rock()) },
            moods: new[] { "low", "weathered", "deliberate" }
        ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "archeology", ["contemplate"] = "iconography" } });

        return area;
    }

    private static Area BuildIrrigationDitch()
    {
        var area = new Area(
            displayName: "Irrigation Ditch",
        referenceLemma: "ditch",
            contextDescription: "at the irrigation ditch",
            transitionDescription: "follow the ditch's edge",
            descriptions: new() { "A shallow ditch cut into the field's edge, water running slowly along it" },
            moods: new[] { "wet", "muddy", "low", "cool" }
        );

        area.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Ditch Bank",
            referenceLemma: "ditch",
            descriptions: new() { "A muddy slope where the ditch's water meets the worked soil" },
            items: new()
            {
                new ItemElement(new Clay()),
                new ItemElement(new Reed()),
            },
            moods: new[] { "muddy", "wet", "cool" }
        ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "drainage", ["smell"] = "scenting" } });

        return area;
    }

    // ── Vegetable mound PoIs ─────────────────────────────────────────────────

    private static PointOfInterest BuildTurnipMound() => new(
        displayName: "Turnip Mound",
        referenceLemma: "turnip",
        descriptions: new() { "A raised mound of turnips, white shoulders pushing through the soil" },
        items: new() { new ItemElement(new Turnip()), new ItemElement(new Turnip()) },
        moods: new[] { "earthy", "rounded" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "seed_lore", ["smell"] = "scenting" } };

    private static PointOfInterest BuildRadishMound() => new(
        displayName: "Radish Mound",
        referenceLemma: "radish",
        descriptions: new() { "A row of radishes, red shoulders showing through" },
        items: new() { new ItemElement(new Radish()), new ItemElement(new Radish()) },
        moods: new[] { "neat", "earthy", "red-topped" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "seed_lore", ["smell"] = "scenting" } };

    private static PointOfInterest BuildParsnipMound() => new(
        displayName: "Parsnip Mound",
        referenceLemma: "parsnip",
        descriptions: new() { "A mound of parsnips, leaves dark and feathery above the soil" },
        items: new() { new ItemElement(new Parsnip()), new ItemElement(new Parsnip()) },
        moods: new[] { "rounded", "dark-leaved" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "seed_lore", ["smell"] = "scenting" } };

    private static PointOfInterest BuildOnionMound() => new(
        displayName: "Onion Mound",
        referenceLemma: "onion",
        descriptions: new() { "A mound of onions, paper-skinned tops nodding in the wind" },
        items: new() { new ItemElement(new Onion()), new ItemElement(new Onion()) },
        moods: new[] { "papery", "yellowing" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "seed_lore", ["smell"] = "scenting" } };

    private static PointOfInterest BuildLeekMound() => new(
        displayName: "Leek Mound",
        referenceLemma: "leek",
        descriptions: new() { "A row of leeks standing tall, dark-green leaves above the soil" },
        items: new() { new ItemElement(new Leek()), new ItemElement(new Leek()) },
        moods: new[] { "tall", "dark-leaved" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "seed_lore", ["smell"] = "scenting" } };

    private static PointOfInterest BuildCabbageMound() => new(
        displayName: "Cabbage Mound",
        referenceLemma: "cabbage",
        descriptions: new() { "A bed of cabbages, leaves curling tight around their cores" },
        items: new() { new ItemElement(new Cabbage()), new ItemElement(new Cabbage()) },
        moods: new[] { "rounded", "green" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "tillage", ["smell"] = "scenting" } };

    private static PointOfInterest BuildPeaMound() => new(
        displayName: "Pea Mound",
        referenceLemma: "pea",
        descriptions: new() { "A row of pea-vines climbing wooden stakes, pods hanging plump" },
        items: new() { new ItemElement(new Pea()), new ItemElement(new Pea()) },
        moods: new[] { "climbing", "tangled" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "tillage", ["smell"] = "scenting" } };

    private static PointOfInterest BuildBeetrootMound() => new(
        displayName: "Beetroot Mound",
        referenceLemma: "beetroot",
        descriptions: new() { "A mound of beetroots, stained-leaf tops above the dark earth" },
        items: new() { new ItemElement(new Beetroot()), new ItemElement(new Beetroot()) },
        moods: new[] { "dark-leaved", "earthy" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "seed_lore", ["smell"] = "scenting" } };

    // ── Herb clump PoIs ──────────────────────────────────────────────────────

    private static PointOfInterest BuildThymeClump() => new(
        displayName: "Thyme Clump",
        referenceLemma: "thyme",
        descriptions: new() { "A low-clinging clump of thyme, fragrant in the warmth" },
        items: new() { new ItemElement(new Thyme()) },
        moods: new[] { "fragrant", "low", "woody" }
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["smell"] = "scenting", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildSageClump() => new(
        displayName: "Sage Clump",
        referenceLemma: "sage",
        descriptions: new() { "A spreading bush of sage, soft grey-green leaves" },
        items: new() { new ItemElement(new Sage()) },
        moods: new[] { "spreading", "grey-green", "soft" }
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["smell"] = "scenting", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildMintClump() => new(
        displayName: "Mint Clump",
        referenceLemma: "mint",
        descriptions: new() { "A vigorous patch of mint, leaves bright green and cool" },
        items: new() { new ItemElement(new Mint()) },
        moods: new[] { "vigorous", "bright", "cool" }
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["smell"] = "scenting", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildChamomileClump() => new(
        displayName: "Chamomile Clump",
        referenceLemma: "chamomile",
        descriptions: new() { "A scatter of low chamomile, white-petalled and golden-centred" },
        items: new() { new ItemElement(new Chamomile()) },
        moods: new[] { "low", "fragrant", "white-flowered" }
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["smell"] = "scenting", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildWormwoodClump() => new(
        displayName: "Wormwood Clump",
        referenceLemma: "wormwood",
        descriptions: new() { "A stand of wormwood, silvered leaves and bitter scent" },
        items: new() { new ItemElement(new Wormwood()) },
        moods: new[] { "silvered", "bitter", "tall" }
    ) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["smell"] = "scenting", ["contemplate"] = "aesthetic" } };

    // ── NPC construction ────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_grainStrip is null || _longhouse is null) return;

        var beds = _longhouse.BedAreas
            .Concat(_barracks?.BedAreas ?? Array.Empty<Area>())
            .ToList();

        var hall      = _longhouse.PublicHall;
        var schedules = new List<NpcSchedule>();

        for (int i = 0; i < _roster.Count && i < beds.Count; i++)
        {
            var archetype = _roster[i];
            var schedule  = BuildScheduleForRole(archetype.ArchetypeId, beds[i], hall, rng);
            schedules.Add(schedule);
            SpawnPeasant(rng, scene, archetype, beds[i], schedule, isReeve: i == 0);
        }

        // Hands may cover the hall; the reeve never is drafted back. They are meant to be there most
        // of the day and out for one period, and an empty field hall during that period is fine.
        if (schedules.Count > 1)
            BuildingSchedule.StaffPublicHall(hall, schedules.Skip(1).ToList());
    
        // Small life. Every location has some; which and how many is rolled, so two
        // places of the same kind are not the same place.
        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Cultivated, 3, 6);
}

    private void SpawnPeasant(
        Random rng, Scene scene, NamedNpcArchetype archetype, Area bed, NpcSchedule schedule, bool isReeve)
    {
        // Affinity persists per NPC: Spawn resolves the table by the NPC's stable id — several
        // same-role peasants (e.g. two plowmen) must never share one table.
        var entity = archetype.Spawn(rng, _grainStrip!.ContextDescription,
            _locationState != null ? _locationState.AffinityFor : null);

        // The reeve answers for the place, so they hold its buildings — without an owner, a witness
        // check indoors falls back to whoever merely has the highest authority level.
        if (isReeve)
        {
            entity.OwnedSectionIds.Add(_longhouse!.Section.Id.ToString());
            if (_barracks != null) entity.OwnedSectionIds.Add(_barracks.Section.Id.ToString());
        }

        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = schedule;

        Console.WriteLine($"FieldSceneFactory: Spawned {entity.DisplayName} ({archetype.ArchetypeId}) — sleeps in {bed.DisplayName}");
    }

    /// <summary>
    /// A field worker's day across the strips they actually work, ending in their own bed in the
    /// longhouse or barracks.
    /// </summary>
    private NpcSchedule BuildScheduleForRole(string archetypeId, Area bed, Area hall, Random rng)
    {
        var grain  = _grainStrip!;
        var veg    = _vegBeds!;
        var ditch  = _ditch!;
        var fallow = _fallow ?? grain;
        var herb   = _herbPatch ?? fallow;

        return archetypeId switch
        {
            // The reeve holds the hall and does business there; the spec allows them out for one
            // period, and an empty hall for that period is acceptable.
            "reeve" => BuildingSchedule.ForWorker(
                bed, hall, new[] { grain, veg, fallow }, rng, awayPeriods: 1),

            "plowman" => BuildingSchedule.ForHand(bed, new[] { grain, grain, grain, ditch }, rng),
            "reaper"  => BuildingSchedule.ForHand(bed, new[] { grain, grain, veg, ditch }, rng),
            "hayward" => BuildingSchedule.ForHand(bed, new[] { ditch, ditch, fallow, grain }, rng),
            _         => BuildingSchedule.ForHand(bed, new[] { veg, veg, ditch, herb }, rng),
        };
    }
}
