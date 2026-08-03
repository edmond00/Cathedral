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
    private Area? _summitApproach, _cirqueBasin;

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

        // The basin lies below the approach and the cliff is its only way in or out, so it stays out
        // of the path chain further down.
        _cirqueBasin = BuildCirqueBasin();
        approachAreas.Add(_cirqueBasin);

        // Populate sparse spots
        foreach (var area in approachAreas.Concat(summitAreas))
            PopulateArea(area, rng);

        var approach = new Section(
            "Summit Approach",
            new() { "Steep, exposed terrain rising toward the peak" },
            seed => new WaveGenerator { Seed = seed }
        );
        approach.Areas.AddRange(approachAreas);
        scene.Sections.Add(approach);
        RegisterAll(scene, approach);

        var summit = new Section(
            "Exposed Summit",
            new() { "Wind-scoured open sky, extreme conditions" },
            seed => new RadiantGenerator { Seed = seed }
        );
        summit.Areas.AddRange(summitAreas);
        scene.Sections.Add(summit);
        RegisterAll(scene, summit);

        _allAreas.AddRange(approachAreas);
        _allAreas.AddRange(summitAreas);

        // Connect linearly — minus the basin, whose only way in is the cliff.
        var walkable = _allAreas.Where(a => a != _cirqueBasin).ToList();

        for (int i = 0; i < walkable.Count - 1; i++)
        {
            var a = walkable[i];
            var b = walkable[i + 1];
            scene.ConnectAreasBidirectional(a, b);
            string name = (a.DisplayName == "Scree Gully" || b.DisplayName == "Scree Gully") ? "Gully"
                        : (a.DisplayName == "Summit Approach" || b.DisplayName == "Summit Approach") ? "Ridge Path"
                        : "Summit Path";
            var path = new PathPointOfInterest(
                a, b, PathPointOfInterest.NameFor(a, b, name),
                new() { $"A narrow path winding from {a.DisplayName.ToLowerInvariant()} to {b.DisplayName.ToLowerInvariant()}" },
                new[] { "narrow", "exposed", "wind-bitten" }
            );
            a.PointsOfInterest.Add(path);
            b.PointsOfInterest.Add(path);
            path.Register(scene);
        }

        // Cliff Descent: Summit Approach → Cirque Basin. The basin is a real place now; the cliff used
        // to point back at the approach as a placeholder for a descent into the mountain location that
        // was never wired, which made the climb a roll with no consequence either way.
        var cliff = new CliffPointOfInterest(
            bottomArea: _cirqueBasin,
            topArea:    _summitApproach,
            displayName: "Cliff Descent",
            descriptions: new() { "A steep cliff dropping away to the basin below, the descent severe" },
            icyCliff:   hasIceShelf,
            moods:      new[] { "sheer", "vertiginous", "exposed", "dangerous" }
        );
        cliff.AttachTo(scene);

        Console.WriteLine($"PeakSceneFactory: peak ({(pointed ? "pointed" : "rounded")}) — {_allAreas.Count} areas");
    
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

            var climbTop = FurnitureSubfactory.AddClimb(
                rng, scene, outdoors[0], FurnitureSubfactory.Setting.Highland);
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

    // ── Area builders ────────────────────────────────────────────────────────

    /// <summary>
    /// A sheltered hollow under the approach, reachable only by the cliff. Being hard to get into is
    /// the whole of its character — it is out of the wind and out of everyone's way.
    /// </summary>
    private static Area BuildCirqueBasin() => new(
        displayName: "Cirque Basin",
        referenceLemma: "basin",
        contextDescription: "down in the cirque basin",
        transitionDescription: "drop into the cirque basin",
        descriptions: new() { "A bowl of still air and old snow scooped out beneath the cliff, walled on three sides" },
        moods: new[] { "still", "sheltered", "cold", "hidden" }
    );

    private static Area BuildSummitApproach() => new(
        displayName: "Summit Approach",
        referenceLemma: "summit",
        contextDescription: "on the summit approach",
        transitionDescription: "begin the summit approach",
        descriptions: new() { "A steep, exposed run of rock and scree leading toward the high places" },
        moods: new[] { "steep", "exposed", "wind-bitten", "thin-aired" }
    );

    private static Area BuildRidge() => new(
        displayName: "Ridge",
        referenceLemma: "ridge",
        contextDescription: "on the narrow ridge",
        transitionDescription: "step onto the ridge",
        descriptions: new() { "A narrow exposed spine of rock with steep falls on either side" },
        moods: new[] { "narrow", "vertiginous", "wind-howling", "thin-aired" }
    );

    private static Area BuildSummitPlateau() => new(
        displayName: "Summit Plateau",
        referenceLemma: "plateau",
        contextDescription: "on the summit plateau",
        transitionDescription: "step onto the summit plateau",
        descriptions: new() { "A flat-topped summit, exposed to the sky on every side" },
        moods: new[] { "flat", "exposed", "vast", "wind-scoured" }
    );

    private static Area BuildWindsweptLedge() => new(
        displayName: "Windswept Ledge",
        referenceLemma: "ledge",
        contextDescription: "on the windswept ledge",
        transitionDescription: "step onto the windswept ledge",
        descriptions: new() { "A jutting shelf of rock overlooking the world below" },
        moods: new[] { "exposed", "vast", "vertiginous", "wind-bitten" }
    );

    private static Area BuildIceShelf() => new(
        displayName: "Ice Shelf",
        referenceLemma: "ice",
        contextDescription: "on the ice shelf",
        transitionDescription: "step onto the ice shelf",
        descriptions: new() { "A frozen shelf of ice glittering against rock and sky" },
        moods: new[] { "frozen", "glittering", "still", "thin-aired" }
    );

    private static Area BuildScreeGully() => new(
        displayName: "Scree Gully",
        referenceLemma: "gully",
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
    
        // Small life. Every location has some; which and how many is rolled, so two
        // places of the same kind are not the same place.
        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Barren, 1, 2);
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

    private void TrySpawnNamed(Random rng, Scene scene, NamedNpcArchetype archetype, double chance)
    {
        if (rng.NextDouble() > chance) return;
        var area = _allAreas[rng.Next(_allAreas.Count)];
        var entity = archetype.Spawn(rng, area.ContextDescription);
        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        // Beasts range: a wolf pinned to one clearing all day is a hazard, not an animal, and there
        // is nothing to track about it. Speaking wilderness folk keep their authored routines.
        scene.NpcSchedules[sceneNpc.Id] = archetype.CanSpeak
            ? NpcSchedule.Always(area)
            : RoamingSchedule(rng, _allAreas);
    }
}
