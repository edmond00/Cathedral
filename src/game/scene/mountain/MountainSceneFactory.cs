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
    private Area? _rockLedge, _boulderField;
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

        _rockLedge    = midAreas.First(a => a.DisplayName == "Rock Ledge");
        _boulderField = midAreas.First(a => a.DisplayName == "Boulder Field");

        foreach (var area in lowerAreas.Concat(midAreas))
            PopulateArea(area, rng);

        // ── Build sections ───────────────────────────────────────────────────

        var lower = new Section(
            "Lower Slope",
            new() { "Transitioning terrain — forest gives way to open rock" }
        );
        lower.Areas.AddRange(lowerAreas);
        scene.Sections.Add(lower);
        RegisterAll(scene, lower);

        var mid = new Section(
            "Rocky Midslope",
            new() { "Exposed and windy — scree, outcrops, sparse vegetation" }
        );
        mid.Areas.AddRange(midAreas);
        scene.Sections.Add(mid);
        RegisterAll(scene, mid);

        _allAreas.AddRange(lowerAreas);
        _allAreas.AddRange(midAreas);

        // ── Connect with PathPoIs (linear chain) ─────────────────────────────

        for (int i = 0; i < _allAreas.Count - 1; i++)
        {
            var a = _allAreas[i];
            var b = _allAreas[i + 1];
            scene.ConnectAreasBidirectional(a, b);
            string name = (a.DisplayName == "Gorge" || b.DisplayName == "Gorge") ? "Gorge Passage"
                        : (a.DisplayName == "Stream Source" || b.DisplayName == "Stream Source") ? "Stream Track"
                        : "Slope Path";
            var path = new PathPointOfInterest(
                a, b, name,
                new() { $"A worn slope path between {a.DisplayName.ToLowerInvariant()} and {b.DisplayName.ToLowerInvariant()}" },
                new[] { "rough", "exposed", "windswept" }
            );
            a.PointsOfInterest.Add(path);
            b.PointsOfInterest.Add(path);
            path.Register(scene);
        }

        // ── Cliff Ascent on Rock Ledge (top area is conceptual; world-level wiring) ─
        // We attach the CliffPoI as a self-referential PoI describing the climb.
        // The parent world graph maintains the cross-location connection to the peak.

        var cliff = new CliffPointOfInterest(
            bottomArea: _rockLedge,
            topArea:    _rockLedge, // same-area placeholder — actual top is in another scene
            displayName: "Cliff Ascent",
            descriptions: new() { "A sheer cliff rising from the ledge toward the peak above, hand-and-foot holds in the rock" },
            icyCliff:   false,
            moods:      new[] { "sheer", "exposed", "vertiginous" }
        );
        _rockLedge.PointsOfInterest.Add(cliff);
        cliff.Register(scene);

        Console.WriteLine($"MountainSceneFactory: {_slope} slope, {_allAreas.Count} areas");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildScreeField() => new(
        displayName: "Scree Field",
        contextDescription: "crossing the scree field",
        transitionDescription: "step onto the scree field",
        descriptions: new() { "A long slope of loose broken stone, treacherous underfoot" },
        moods: new[] { "loose", "grey", "treacherous", "exposed" }
    );

    private static Area BuildRockLedge() => new(
        displayName: "Rock Ledge",
        contextDescription: "on the rock ledge",
        transitionDescription: "step onto the rock ledge",
        descriptions: new() { "A flat shelf of rock with a wide view across the country below" },
        moods: new[] { "exposed", "windswept", "wide-open", "high" }
    );

    private static Area BuildAlpineMeadow() => new(
        displayName: "Alpine Meadow",
        contextDescription: "in the alpine meadow",
        transitionDescription: "step into the alpine meadow",
        descriptions: new() { "A sheltered hollow of unexpected greenery, herbs growing in soft drifts" },
        moods: new[] { "sheltered", "fragrant", "green", "still" }
    );

    private static Area BuildGorge() => new(
        displayName: "Gorge",
        contextDescription: "in the narrow gorge",
        transitionDescription: "enter the narrow gorge",
        descriptions: new() { "A narrow cut between rock walls, a stream running at its base" },
        moods: new[] { "narrow", "echoing", "wet", "cool" }
    );

    private static Area BuildBoulderField() => new(
        displayName: "Boulder Field",
        contextDescription: "in the boulder field",
        transitionDescription: "pick a way through the boulder field",
        descriptions: new() { "A jumble of massive rocks the size of houses, paths winding between" },
        moods: new[] { "massive", "stilled", "exposed", "monumental" }
    );

    private static Area BuildStreamSource() => new(
        displayName: "Stream Source",
        contextDescription: "at the stream source",
        transitionDescription: "approach the stream source",
        descriptions: new() { "A spring breaks from the rock, water cold and bright" },
        moods: new[] { "cold", "bright", "running", "fresh" }
    );

    private static Area BuildSlopeForest() => new(
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
    }

    private void TrySpawnNamed(Random rng, Scene scene, NamedNpcArchetype archetype, double chance)
    {
        if (rng.NextDouble() > chance) return;
        var area = _allAreas[rng.Next(_allAreas.Count)];
        var entity = archetype.Spawn(rng, area.ContextDescription);
        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = NpcSchedule.Always(area.DisplayName.ToLowerInvariant());
    }

    private void SpawnShallow(Random rng, Scene scene, ShallowNpcArchetype archetype)
    {
        var area = _allAreas[rng.Next(_allAreas.Count)];
        var entity = archetype.Spawn(rng, area.DisplayName.ToLowerInvariant());
        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = NpcSchedule.Always(area.DisplayName.ToLowerInvariant());
    }

    private void TrySpawnShallow(Random rng, Scene scene, ShallowNpcArchetype archetype, double chance)
    {
        if (rng.NextDouble() > chance) return;
        SpawnShallow(rng, scene, archetype);
    }
}
