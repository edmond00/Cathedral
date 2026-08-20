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

namespace Cathedral.Game.Scene.Mountain;

/// <summary>
/// Builds a procedural mountain scene per the v1 world-content spec (mountain.md).
///
/// Slope character: sunny (more alpine meadow / herbs) vs damp (more gorge / moss).
/// Sections: Lower Slope, Rocky Midslope.
/// Areas (3–4): Scree Field, Rock Ledge, Alpine Meadow, Gorge, Boulder Field,
/// Stream Source, Slope Forest.
/// CliffPoI ("Cliff Ascent") on Rock Ledge → Peak (registered by parent world graph).
/// 50 % chance of a Door connection from Boulder Field → Cave (also parent-handled).
/// </summary>
public class MountainSceneFactory : SceneFactory
{
    public MountainSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum Slope { Sunny, Damp }

    private Slope _slope;
    private Area? _rockLedge, _boulderField, _highCrag;
    private readonly List<Area> _allAreas = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        _slope = rng.NextDouble() < 0.5 ? Slope.Sunny : Slope.Damp;
        bool hasGorge = rng.NextDouble() < 0.4;

        // ── Sample areas ─────────────────────────────────────────────────────

        var lowerBuilders = new List<(string id, Func<Area> builder)>
        {
            ("scree_field",   BuildScreeField),
            ("slope_forest",  BuildSlopeForest),
        };
        var midBuilders = new List<(string id, Func<Area> builder)>
        {
            ("rock_ledge",    BuildRockLedge),
            ("boulder_field", BuildBoulderField),
        };
        if (_slope == Slope.Sunny)
            midBuilders.Add(("alpine_meadow", BuildAlpineMeadow));
        if (hasGorge)
        {
            midBuilders.Add(("gorge", BuildGorge));
            midBuilders.Add(("stream_source", BuildStreamSource));
        }

        int total = rng.Next(3, 5);
        int lowerCount = Math.Min(rng.Next(1, 3), lowerBuilders.Count);
        int midCount   = Math.Min(total - lowerCount, midBuilders.Count);

        var lowerAreas = SampleUniqueIndices(rng, lowerBuilders.Count, lowerCount)
            .Select(i => lowerBuilders[i].builder()).ToList();
        var midAreas = SampleUniqueIndices(rng, midBuilders.Count, midCount)
            .Select(i => midBuilders[i].builder()).ToList();

        // Always include Rock Ledge (cliff ascent anchor) and Boulder Field
        if (!midAreas.Any(a => a.DisplayName == "Rock Ledge"))
            midAreas.Add(BuildRockLedge());
        if (!midAreas.Any(a => a.DisplayName == "Boulder Field"))
            midAreas.Add(BuildBoulderField());

        // The crag sits above the ledge and is reachable only by the cliff, so it is deliberately
        // left out of the path chain below — it is the one area here you have to climb to.
        midAreas.Add(BuildHighCrag());

        _rockLedge    = midAreas.First(a => a.DisplayName == "Rock Ledge");
        _boulderField = midAreas.First(a => a.DisplayName == "Boulder Field");
        _highCrag     = midAreas.First(a => a.DisplayName == "High Crag");

        foreach (var area in lowerAreas.Concat(midAreas))
            PopulateArea(area, rng);

        // ── Build sections ───────────────────────────────────────────────────

        var lower = new Section(
            "Lower Slope",
            new() { "Transitioning terrain — forest gives way to open rock" },
            seed => new NoisyGenerator { Seed = seed, Density = 0.7f }
        );
        lower.Areas.AddRange(lowerAreas);
        scene.Sections.Add(lower);
        RegisterAll(scene, lower);

        var mid = new Section(
            "Rocky Midslope",
            new() { "Exposed and windy — scree, outcrops, sparse vegetation" },
            seed => new WaveGenerator { Seed = seed }
        );
        mid.Areas.AddRange(midAreas);
        scene.Sections.Add(mid);
        RegisterAll(scene, mid);

        _allAreas.AddRange(lowerAreas);
        _allAreas.AddRange(midAreas);

        // ── Connect with PathPoIs (linear chain) ─────────────────────────────

        // The High Crag is left out of the chain: the cliff below is its only way in, and a path to it
        // would be exactly the free bypass the connector exists to prevent.
        var walkable = _allAreas.Where(a => a != _highCrag).ToList();

        for (int i = 0; i < walkable.Count - 1; i++)
        {
            var a = walkable[i];
            var b = walkable[i + 1];
            scene.ConnectAreasBidirectional(a, b);
            string name = (a.DisplayName == "Gorge" || b.DisplayName == "Gorge") ? "Gorge Passage"
                        : (a.DisplayName == "Stream Source" || b.DisplayName == "Stream Source") ? "Stream Track"
                        : "Slope Path";
            var path = new PathPointOfInterest(
                a, b, PathPointOfInterest.NameFor(a, b, name),
                new() { $"A worn slope path between {a.DisplayName.ToLowerInvariant()} and {b.DisplayName.ToLowerInvariant()}" },
                new[] { "rough", "exposed", "windswept" }
            );
            a.PointsOfInterest.Add(path);
            b.PointsOfInterest.Add(path);
            path.Register(scene);
        }

        // ── Cliff Ascent: Rock Ledge → High Crag ─────────────────────────────
        // The crag is a real area, reachable only by the climb. It used to be a self-referential
        // placeholder pointing back at the ledge — the verb appeared, the roll happened, and the
        // player did not move — on the theory that the true top was the peak location next door.
        // Nothing ever wired that up, so the climb was a no-op for as long as it existed.

        var cliff = new CliffPointOfInterest(
            bottomArea: _rockLedge,
            topArea:    _highCrag,
            displayName: "Cliff Ascent",
            descriptions: new() { "A sheer cliff rising from the ledge toward the peak above, hand-and-foot holds in the rock" },
            icyCliff:   false,
            moods:      new[] { "sheer", "exposed", "vertiginous" }
        );
        cliff.AttachTo(scene);

        Console.WriteLine($"MountainSceneFactory: {_slope} slope, {_allAreas.Count} areas");
    
        // ── Furnishing: somewhere to sit, somewhere to hide, a hard shortcut, a climb ──
        // Rolled, so two places of the same kind are not the same place. Runs after the sections and
        // paths exist: shortcuts need to know what is already adjacent, and the climb needs a section
        // to put its top area in.
        {
            var outdoors = scene.OutdoorAreas;
            FurnitureSubfactory.AddSitSpots(rng, outdoors, FurnitureSubfactory.Setting.Highland);
            FurnitureSubfactory.AddHidingPlaces(rng, outdoors, FurnitureSubfactory.Setting.Highland);
            FurnitureSubfactory.AddShortcuts(rng, scene, outdoors, FurnitureSubfactory.Setting.Highland);
            FurnitureSubfactory.AddExtractionPoints(rng, outdoors, FurnitureSubfactory.Setting.Highland);

        }

        // The high ground sees the whole place. The high crag is the top of the cliff, so
        // reaching it costs a climb — and what it buys is a road to everywhere else here, which is
        // the entire bargain of going up. Placed by hand rather than found: the old automatic pass
        // named whichever areas were built first, which from a summit is not a view, it is a list.
        if (_highCrag != null)
            AddLandscapes(scene, _highCrag, scene.AllAreas);
}

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildScreeField() => new ScreeArea(
        displayName: "Scree Field",
        contextDescription: "crossing the scree field",
        transitionDescription: "step onto the scree field",
        descriptions: new() { "A long slope of loose broken stone, treacherous underfoot" },
        moods: new[] { "loose", "grey", "treacherous", "exposed" }
    );

    private static Area BuildRockLedge() => new LedgeArea(
        displayName: "Rock Ledge",
        contextDescription: "on the rock ledge",
        transitionDescription: "step onto the rock ledge",
        descriptions: new() { "A flat shelf of rock with a wide view across the country below" },
        moods: new[] { "exposed", "windswept", "wide-open", "high" }
    );

    /// <summary>
    /// The one area on the mountain reached only by climbing. Deliberately bare — what it is for is
    /// the view, not what is lying about on it.
    /// </summary>
    private static Area BuildHighCrag() => new CragArea(
        displayName: "High Crag",
        contextDescription: "on the high crag above the ledge",
        transitionDescription: "pull yourself onto the high crag",
        descriptions: new() { "A wind-scoured spur of rock standing clear of the slope, with nothing above it but the peak" },
        moods: new[] { "wind-scoured", "airy", "bare", "commanding" }
    );

    private static Area BuildAlpineMeadow() => new MeadowArea(
        displayName: "Alpine Meadow",
        contextDescription: "in the alpine meadow",
        transitionDescription: "step into the alpine meadow",
        descriptions: new() { "A sheltered hollow of unexpected greenery, herbs growing in soft drifts" },
        moods: new[] { "sheltered", "fragrant", "green", "still" }
    );

    private static Area BuildGorge() => new GorgeArea(
        displayName: "Gorge",
        contextDescription: "in the narrow gorge",
        transitionDescription: "enter the narrow gorge",
        descriptions: new() { "A narrow cut between rock walls, a stream running at its base" },
        moods: new[] { "narrow", "echoing", "wet", "cool" }
    );

    private static Area BuildBoulderField() => new BoulderArea(
        displayName: "Boulder Field",
        contextDescription: "in the boulder field",
        transitionDescription: "pick a way through the boulder field",
        descriptions: new() { "A jumble of massive rocks the size of houses, paths winding between" },
        moods: new[] { "massive", "stilled", "exposed", "monumental" }
    );

    private static Area BuildStreamSource() => new StreamArea(
        displayName: "Stream Source",
        contextDescription: "at the stream source",
        transitionDescription: "approach the stream source",
        descriptions: new() { "A spring breaks from the rock, water cold and bright" },
        moods: new[] { "cold", "bright", "running", "fresh" }
    );

    private static Area BuildSlopeForest() => new ForestArea(
        displayName: "Slope Forest",
        contextDescription: "in the slope forest",
        transitionDescription: "step into the slope forest",
        descriptions: new() { "Dense wood climbs the lower slope, pine giving way to scrub above" },
        moods: new[] { "tilted", "rooted", "shaded", "tall" }
    );

    // ── Spot population ──────────────────────────────────────────────────────

    private void PopulateArea(Area area, Random rng)
    {
        switch (area.DisplayName)
        {
            case "Scree Field":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildFallenRocks());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildLichenCrust());
                break;
            case "Rock Ledge":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildRockOutcrop());
                if (_slope == Slope.Sunny && rng.NextDouble() < 0.5)
                    area.PointsOfInterest.Add(TerrainSubfactory.BuildAlpineHerbPatch());
                break;
            case "Alpine Meadow":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildAlpineHerbPatch());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildFlowerPatch());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(TerrainSubfactory.BuildBerryBush());
                break;
            case "Gorge":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildGorgePool());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildRockFace());
                break;
            case "Boulder Field":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildBoulder());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildBoulder());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildCrevice());
                break;
            case "Stream Source":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildStreamBank());
                break;
            case "Slope Forest":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildPineTree());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildPineTree());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildMossBank());
                break;
        }
    }

    // ── NPC construction ────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allAreas.Count == 0) return;

        // Beasts
        TrySpawnNamed(rng, scene, new WolfArchetype(), 0.20);
        TrySpawnNamed(rng, scene, new BoarArchetype(), 0.20);

        // Shallow wildlife — Eagle always
        SpawnShallow(rng, scene, new EagleArchetype());
        TrySpawnShallow(rng, scene, new MountainGoatArchetype(), 0.50);
        TrySpawnShallow(rng, scene, new MarmotArchetype(),       0.40);
        TrySpawnShallow(rng, scene, new RavenArchetype(),        0.50);
        TrySpawnShallow(rng, scene, new AdderArchetype(),        0.25);
        TrySpawnShallow(rng, scene, new LynxArchetype(),         0.15);
    
        // Small life. Every location has some; which and how many is rolled, so two
        // places of the same kind are not the same place.
        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Barren, 1, 3);
}

    private void TrySpawnNamed(Random rng, Scene scene, NamedNpcArchetype archetype, double chance)
    {
        if (rng.NextDouble() > chance) return;
        var area = _allAreas[rng.Next(_allAreas.Count)];
        // Affinity persists per NPC: Spawn resolves the table by the NPC's stable id. Without the
        // resolver every rebuild hands out a fresh table, so a beast appeased here is hostile again
        // on the next arrival while DepartedNpcs still remembers the ones that died.
        var entity = archetype.Spawn(rng, area.ContextDescription,
            _locationState != null ? _locationState.AffinityFor : null);
        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        // Beasts range: a wolf pinned to one clearing all day is a hazard, not an animal, and there
        // is nothing to track about it. Speaking wilderness folk keep their authored routines.
        scene.NpcSchedules[sceneNpc.Id] = archetype.CanSpeak
            ? NpcSchedule.Always(area)
            : RoamingSchedule(rng, _allAreas);
    }

    private void SpawnShallow(Random rng, Scene scene, ShallowNpcArchetype archetype)
    {
        var area = _allAreas[rng.Next(_allAreas.Count)];
        var entity = archetype.Spawn(rng, area.DisplayName.ToLowerInvariant());
        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = NpcSchedule.Always(area);
    }

    private void TrySpawnShallow(Random rng, Scene scene, ShallowNpcArchetype archetype, double chance)
    {
        if (rng.NextDouble() > chance) return;
        SpawnShallow(rng, scene, archetype);
    }
}
