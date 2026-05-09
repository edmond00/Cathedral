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

namespace Cathedral.Game.Scene.Peak;

/// <summary>
/// Builds a procedural mountain-peak scene per the v1 world-content spec (peak.md).
///
/// Sections: Summit Approach, Exposed Summit.
/// Areas (2–3): Ridge, Summit Plateau, Windswept Ledge, Ice Shelf, Scree Gully.
/// Sparse spots: Cairn (60 %), Wind-Carved Rock, Crevice, Ice Formation.
/// Eagle always present. Reward = view + rare herbs.
/// CliffPoI "Cliff Descent" on Summit Approach → Mountain (parent-handled).
/// </summary>
public class PeakSceneFactory : SceneFactory
{
    public PeakSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private readonly List<Area> _allAreas = new();
    private Area? _summitApproach;

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        bool pointed = rng.NextDouble() < 0.5; // pointed (ridge + ledge) vs rounded (plateau + ledge)
        bool hasIceShelf = rng.NextDouble() < 0.20;

        // Summit Approach is always present (cliff descent anchor)
        _summitApproach = BuildSummitApproach();

        // Build summit areas: 1–2
        var summitAreas = new List<Area>();
        if (pointed)
        {
            summitAreas.Add(BuildRidge());
            summitAreas.Add(BuildWindsweptLedge());
        }
        else
        {
            summitAreas.Add(BuildSummitPlateau());
            if (rng.NextDouble() < 0.5) summitAreas.Add(BuildWindsweptLedge());
        }
        if (hasIceShelf) summitAreas.Add(BuildIceShelf());

        var approachAreas = new List<Area> { _summitApproach };
        if (rng.NextDouble() < 0.4) approachAreas.Add(BuildScreeGully());

        // Populate sparse spots
        foreach (var area in approachAreas.Concat(summitAreas))
            PopulateArea(area, rng);

        var approach = new Section(
            "Summit Approach",
            new() { "Steep, exposed terrain rising toward the peak" }
        );
        approach.Areas.AddRange(approachAreas);
        scene.Sections.Add(approach);
        RegisterAll(scene, approach);

        var summit = new Section(
            "Exposed Summit",
            new() { "Wind-scoured open sky, extreme conditions" }
        );
        summit.Areas.AddRange(summitAreas);
        scene.Sections.Add(summit);
        RegisterAll(scene, summit);

        _allAreas.AddRange(approachAreas);
        _allAreas.AddRange(summitAreas);

        // Connect linearly
        for (int i = 0; i < _allAreas.Count - 1; i++)
        {
            var a = _allAreas[i];
            var b = _allAreas[i + 1];
            scene.ConnectAreasBidirectional(a, b);
            string name = (a.DisplayName == "Scree Gully" || b.DisplayName == "Scree Gully") ? "Gully"
                        : (a.DisplayName == "Summit Approach" || b.DisplayName == "Summit Approach") ? "Ridge Path"
                        : "Summit Path";
            var path = new PathPointOfInterest(
                a, b, name,
                new() { $"A narrow path winding from {a.DisplayName.ToLowerInvariant()} to {b.DisplayName.ToLowerInvariant()}" },
                new[] { "narrow", "exposed", "wind-bitten" }
            );
            a.PointsOfInterest.Add(path);
            b.PointsOfInterest.Add(path);
            path.Register(scene);
        }

        // Cliff Descent (down to Mountain — placeholder)
        var cliff = new CliffPointOfInterest(
            bottomArea: _summitApproach,
            topArea:    _summitApproach, // self-referential placeholder
            displayName: "Cliff Descent",
            descriptions: new() { "A steep cliff dropping away to the slopes below, the descent severe" },
            icyCliff:   hasIceShelf,
            moods:      new[] { "sheer", "vertiginous", "exposed", "dangerous" }
        );
        _summitApproach.PointsOfInterest.Add(cliff);
        cliff.Register(scene);

        Console.WriteLine($"PeakSceneFactory: peak ({(pointed ? "pointed" : "rounded")}) — {_allAreas.Count} areas");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildSummitApproach() => new(
        displayName: "Summit Approach",
        contextDescription: "on the summit approach",
        transitionDescription: "begin the summit approach",
        descriptions: new() { "A steep, exposed run of rock and scree leading toward the high places" },
        moods: new[] { "steep", "exposed", "wind-bitten", "thin-aired" }
    );

    private static Area BuildRidge() => new(
        displayName: "Ridge",
        contextDescription: "on the narrow ridge",
        transitionDescription: "step onto the ridge",
        descriptions: new() { "A narrow exposed spine of rock with steep falls on either side" },
        moods: new[] { "narrow", "vertiginous", "wind-howling", "thin-aired" }
    );

    private static Area BuildSummitPlateau() => new(
        displayName: "Summit Plateau",
        contextDescription: "on the summit plateau",
        transitionDescription: "step onto the summit plateau",
        descriptions: new() { "A flat-topped summit, exposed to the sky on every side" },
        moods: new[] { "flat", "exposed", "vast", "wind-scoured" }
    );

    private static Area BuildWindsweptLedge() => new(
        displayName: "Windswept Ledge",
        contextDescription: "on the windswept ledge",
        transitionDescription: "step onto the windswept ledge",
        descriptions: new() { "A jutting shelf of rock overlooking the world below" },
        moods: new[] { "exposed", "vast", "vertiginous", "wind-bitten" }
    );

    private static Area BuildIceShelf() => new(
        displayName: "Ice Shelf",
        contextDescription: "on the ice shelf",
        transitionDescription: "step onto the ice shelf",
        descriptions: new() { "A frozen shelf of ice glittering against rock and sky" },
        moods: new[] { "frozen", "glittering", "still", "thin-aired" }
    );

    private static Area BuildScreeGully() => new(
        displayName: "Scree Gully",
        contextDescription: "in the scree gully",
        transitionDescription: "descend into the scree gully",
        descriptions: new() { "A steep loose-rock channel cutting down the side of the peak" },
        moods: new[] { "loose", "treacherous", "narrow", "steep" }
    );

    // ── Spot population ──────────────────────────────────────────────────────

    private void PopulateArea(Area area, Random rng)
    {
        // Sparse — peak should feel exposed and minimal
        switch (area.DisplayName)
        {
            case "Summit Approach":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildRockOutcrop());
                break;
            case "Ridge":
            case "Summit Plateau":
            case "Windswept Ledge":
                if (rng.NextDouble() < 0.6) area.PointsOfInterest.Add(TerrainSubfactory.BuildCairn());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildLichenCrust());
                if (rng.NextDouble() < 0.4) area.PointsOfInterest.Add(TerrainSubfactory.BuildShelteredHollow());
                break;
            case "Ice Shelf":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildIceFormation());
                break;
            case "Scree Gully":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildFallenRocks());
                break;
        }
    }

    // ── NPC construction ────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allAreas.Count == 0) return;

        // Eagle always
        SpawnShallow(rng, scene, new EagleArchetype());

        TrySpawnShallow(rng, scene, new RavenArchetype(),         0.50);
        TrySpawnShallow(rng, scene, new SnowHareArchetype(),      0.20);
        TrySpawnShallow(rng, scene, new MountainGoatArchetype(),  0.30);
        TrySpawnNamed  (rng, scene, new WolfArchetype(),          0.10);
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
}
