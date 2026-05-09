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

namespace Cathedral.Game.Scene.Field;

/// <summary>
/// Builds a procedural cultivated-field scene per the v1 world-content spec (field.md).
///
/// Sections: Tilled Strips, Field Margin.
/// Areas (5 picked): Grain Strip (one of wheat/barley/rye, or flax in 10% of fields),
/// Vegetable Beds, optional Herb Patch (30%), Fallow Ground (50%), Irrigation Ditch.
/// Connections: <see cref="PathPointOfInterest"/> field tracks.
/// NPCs: Reeve, Plowman×1–2, Reaper×1–3, Hayward, Bondman×1–3 (return to farm/village at night).
/// </summary>
public class FieldSceneFactory : SceneFactory
{
    public FieldSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum GrainCrop { Wheat, Barley, Rye, Flax }

    private GrainCrop _crop;
    private Area? _grainStrip, _vegBeds, _herbPatch, _fallow, _ditch;
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
            new() { "Cultivated rows of crop running long across the worked ground" }
        );
        tilled.Areas.Add(_grainStrip);
        tilled.Areas.Add(_vegBeds);
        if (_herbPatch != null) tilled.Areas.Add(_herbPatch);
        scene.Sections.Add(tilled);
        RegisterAll(scene, tilled);

        var margin = new Section(
            "Field Margin",
            new() { "The boundary strip between cultivation and wild ground" }
        );
        margin.Areas.Add(_ditch);
        if (_fallow != null) margin.Areas.Add(_fallow);
        scene.Sections.Add(margin);
        RegisterAll(scene, margin);

        _allAreas.AddRange(tilled.Areas);
        _allAreas.AddRange(margin.Areas);

        // ── 3. Connect with PathPoIs ─────────────────────────────────────────

        ConnectAreas(scene, _grainStrip, _vegBeds, "Field Track");
        if (_herbPatch != null)
            ConnectAreas(scene, _vegBeds, _herbPatch, "Field Track");
        ConnectAreas(scene, _grainStrip, _ditch, "Ditch Edge");
        if (_fallow != null)
            ConnectAreas(scene, _fallow, _ditch, "Margin Path");

        Console.WriteLine($"FieldSceneFactory: Built field — crop={_crop}, areas={_allAreas.Count}");
    }

    private static void ConnectAreas(Scene scene, Area a, Area b, string pathName)
    {
        scene.ConnectAreasBidirectional(a, b);
        var path = new PathPointOfInterest(
            a, b, pathName,
            new() { $"A worn track running between {a.DisplayName.ToLowerInvariant()} and {b.DisplayName.ToLowerInvariant()}" },
            new[] { "worn", "narrow", "cropped" }
        );
        a.PointsOfInterest.Add(path);
        b.PointsOfInterest.Add(path);
        path.Register(scene);
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
            descriptions: new() { _crop == GrainCrop.Flax ? "A row of pale flax stems, ready for harvest" : "A row of grain stems, ears heavy" },
            items: cropItems,
            moods: new[] { "long", "rustling", "ripe" }
        ));

        // Scarecrow
        area.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Scarecrow",
            descriptions: new() { "A straw-stuffed figure on a pole, hat slumped, sleeves blowing" },
            items: new()
            {
                new ItemElement(new Straw()),
                new ItemElement(new Rope()),
            },
            moods: new[] { "tattered", "lonely", "still", "wind-blown" }
        ));

        // Tool Rest with sickle and rake
        area.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Tool Rest",
            descriptions: new() { "A wooden bench at the field-edge with tools leaning against it" },
            items: new()
            {
                new ItemElement(new Sickle()),
                new ItemElement(new Rake()),
            },
            moods: new[] { "low", "worn", "dusty" }
        ));

        return area;
    }

    private static Area BuildVegetableBeds(Random rng)
    {
        var area = new Area(
            displayName: "Vegetable Beds",
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
            contextDescription: "on the fallow ground",
            transitionDescription: "walk onto the fallow ground",
            descriptions: new() { "A resting strip of unworked land, weeds and grass reclaiming the furrows" },
            moods: new[] { "weedy", "still", "loose-soiled", "quiet" }
        );

        area.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Stone Marker",
            descriptions: new() { "A flat stone set in the earth, marking a boundary" },
            items: new() { new ItemElement(new Rock()) },
            moods: new[] { "low", "weathered", "deliberate" }
        ));

        return area;
    }

    private static Area BuildIrrigationDitch()
    {
        var area = new Area(
            displayName: "Irrigation Ditch",
            contextDescription: "at the irrigation ditch",
            transitionDescription: "follow the ditch's edge",
            descriptions: new() { "A shallow ditch cut into the field's edge, water running slowly along it" },
            moods: new[] { "wet", "muddy", "low", "cool" }
        );

        area.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Ditch Bank",
            descriptions: new() { "A muddy slope where the ditch's water meets the worked soil" },
            items: new()
            {
                new ItemElement(new Clay()),
                new ItemElement(new Reed()),
            },
            moods: new[] { "muddy", "wet", "cool" }
        ));

        return area;
    }

    // ── Vegetable mound PoIs ─────────────────────────────────────────────────

    private static PointOfInterest BuildTurnipMound() => new(
        displayName: "Turnip Mound",
        descriptions: new() { "A raised mound of turnips, white shoulders pushing through the soil" },
        items: new() { new ItemElement(new Turnip()), new ItemElement(new Turnip()) },
        moods: new[] { "earthy", "rounded" }
    );

    private static PointOfInterest BuildRadishMound() => new(
        displayName: "Radish Mound",
        descriptions: new() { "A row of radishes, red shoulders showing through" },
        items: new() { new ItemElement(new Radish()), new ItemElement(new Radish()) },
        moods: new[] { "neat", "earthy", "red-topped" }
    );

    private static PointOfInterest BuildParsnipMound() => new(
        displayName: "Parsnip Mound",
        descriptions: new() { "A mound of parsnips, leaves dark and feathery above the soil" },
        items: new() { new ItemElement(new Parsnip()), new ItemElement(new Parsnip()) },
        moods: new[] { "rounded", "dark-leaved" }
    );

    private static PointOfInterest BuildOnionMound() => new(
        displayName: "Onion Mound",
        descriptions: new() { "A mound of onions, paper-skinned tops nodding in the wind" },
        items: new() { new ItemElement(new Onion()), new ItemElement(new Onion()) },
        moods: new[] { "papery", "yellowing" }
    );

    private static PointOfInterest BuildLeekMound() => new(
        displayName: "Leek Mound",
        descriptions: new() { "A row of leeks standing tall, dark-green leaves above the soil" },
        items: new() { new ItemElement(new Leek()), new ItemElement(new Leek()) },
        moods: new[] { "tall", "dark-leaved" }
    );

    private static PointOfInterest BuildCabbageMound() => new(
        displayName: "Cabbage Mound",
        descriptions: new() { "A bed of cabbages, leaves curling tight around their cores" },
        items: new() { new ItemElement(new Cabbage()), new ItemElement(new Cabbage()) },
        moods: new[] { "rounded", "green" }
    );

    private static PointOfInterest BuildPeaMound() => new(
        displayName: "Pea Mound",
        descriptions: new() { "A row of pea-vines climbing wooden stakes, pods hanging plump" },
        items: new() { new ItemElement(new Pea()), new ItemElement(new Pea()) },
        moods: new[] { "climbing", "tangled" }
    );

    private static PointOfInterest BuildBeetrootMound() => new(
        displayName: "Beetroot Mound",
        descriptions: new() { "A mound of beetroots, stained-leaf tops above the dark earth" },
        items: new() { new ItemElement(new Beetroot()), new ItemElement(new Beetroot()) },
        moods: new[] { "dark-leaved", "earthy" }
    );

    // ── Herb clump PoIs ──────────────────────────────────────────────────────

    private static PointOfInterest BuildThymeClump() => new(
        displayName: "Thyme Clump",
        descriptions: new() { "A low-clinging clump of thyme, fragrant in the warmth" },
        items: new() { new ItemElement(new Thyme()) },
        moods: new[] { "fragrant", "low", "woody" }
    );

    private static PointOfInterest BuildSageClump() => new(
        displayName: "Sage Clump",
        descriptions: new() { "A spreading bush of sage, soft grey-green leaves" },
        items: new() { new ItemElement(new Sage()) },
        moods: new[] { "spreading", "grey-green", "soft" }
    );

    private static PointOfInterest BuildMintClump() => new(
        displayName: "Mint Clump",
        descriptions: new() { "A vigorous patch of mint, leaves bright green and cool" },
        items: new() { new ItemElement(new Mint()) },
        moods: new[] { "vigorous", "bright", "cool" }
    );

    private static PointOfInterest BuildChamomileClump() => new(
        displayName: "Chamomile Clump",
        descriptions: new() { "A scatter of low chamomile, white-petalled and golden-centred" },
        items: new() { new ItemElement(new Chamomile()) },
        moods: new[] { "low", "fragrant", "white-flowered" }
    );

    private static PointOfInterest BuildWormwoodClump() => new(
        displayName: "Wormwood Clump",
        descriptions: new() { "A stand of wormwood, silvered leaves and bitter scent" },
        items: new() { new ItemElement(new Wormwood()) },
        moods: new[] { "silvered", "bitter", "tall" }
    );

    // ── NPC construction ────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_grainStrip is null) return;

        // Reeve (always) + 1-2 plowmen + 1-3 reapers + hayward + 1-3 bondmen
        SpawnPeasant(rng, scene, new ReeveArchetype(),     _grainStrip);

        int plowmen = rng.Next(1, 3);
        for (int i = 0; i < plowmen; i++)
            SpawnPeasant(rng, scene, new PlowmanArchetype(), _grainStrip);

        int reapers = rng.Next(1, 4);
        for (int i = 0; i < reapers; i++)
            SpawnPeasant(rng, scene, new ReaperArchetype(), _grainStrip);

        SpawnPeasant(rng, scene, new HaywardArchetype(),   _grainStrip);

        int bondmen = rng.Next(1, 4);
        for (int i = 0; i < bondmen; i++)
            SpawnPeasant(rng, scene, new BondmanArchetype(), _vegBeds!);
    }

    private void SpawnPeasant(Random rng, Scene scene, NamedNpcArchetype archetype, Area defaultArea)
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
        var entity = archetype.Spawn(rng, defaultArea.ContextDescription, saved);
        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = BuildScheduleForRole(archetype.ArchetypeId);
    }

    private NpcSchedule BuildScheduleForRole(string archetypeId)
    {
        var grainId   = _grainStrip!.DisplayName.ToLowerInvariant();
        var vegId     = _vegBeds!.DisplayName.ToLowerInvariant();
        var herbId    = _herbPatch?.DisplayName.ToLowerInvariant();
        var fallowId  = _fallow?.DisplayName.ToLowerInvariant() ?? grainId;
        var ditchId   = _ditch!.DisplayName.ToLowerInvariant();

        // All workers return to farm/village at night → schedule omits Night.
        return archetypeId switch
        {
            "reeve" => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = grainId,
                [TimePeriod.Morning]   = grainId,
                [TimePeriod.Noon]      = fallowId,
                [TimePeriod.Afternoon] = vegId,
                [TimePeriod.Evening]   = grainId,
                [TimePeriod.Night]     = null,
            }),

            "plowman" => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = grainId,
                [TimePeriod.Morning]   = grainId,
                [TimePeriod.Noon]      = ditchId,
                [TimePeriod.Afternoon] = grainId,
                [TimePeriod.Evening]   = grainId,
                [TimePeriod.Night]     = null,
            }),

            "reaper" => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = null,
                [TimePeriod.Morning]   = grainId,
                [TimePeriod.Noon]      = ditchId,
                [TimePeriod.Afternoon] = grainId,
                [TimePeriod.Evening]   = vegId,
                [TimePeriod.Night]     = null,
            }),

            "hayward" => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = null,
                [TimePeriod.Morning]   = ditchId,
                [TimePeriod.Noon]      = fallowId,
                [TimePeriod.Afternoon] = ditchId,
                [TimePeriod.Evening]   = grainId,
                [TimePeriod.Night]     = null,
            }),

            _ /* bondman */ => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = null,
                [TimePeriod.Morning]   = vegId,
                [TimePeriod.Noon]      = ditchId,
                [TimePeriod.Afternoon] = herbId ?? fallowId,
                [TimePeriod.Evening]   = vegId,
                [TimePeriod.Night]     = null,
            }),
        };
    }
}
