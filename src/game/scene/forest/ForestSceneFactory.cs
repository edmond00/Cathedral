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

namespace Cathedral.Game.Scene.Forest;

/// <summary>
/// Builds a procedural forest scene per the v1 world-content spec (forest.md).
///
/// Identity: deciduous (oak/beech/ash), coniferous (pine), or mixed — gates tree pool.
/// Sections: Forest Edge, Deep Wood.
/// Areas (3–5): Clearing, Thicket, Old Growth, Streamside, Deadwood Patch, Slope Section.
/// Camp PoIs (forest camp) added to Clearing when a woodcutter spawns (~30%).
/// </summary>
public class ForestSceneFactory : SceneFactory
{
    public ForestSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum ForestType { Deciduous, Coniferous, Mixed }

    private ForestType _type;
    private bool _hasWoodcutter;
    private bool _hasCharcoalBurner;
    private bool _hasStream;
    private Area? _clearing;
    private readonly List<Area> _allAreas = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        _type             = (ForestType)rng.Next(3);
        _hasWoodcutter    = rng.NextDouble() < 0.30;
        _hasCharcoalBurner = rng.NextDouble() < 0.10;
        _hasStream        = rng.NextDouble() < 0.50;

        // ── Sample 3–5 areas; Clearing is always present ─────────────────────

        var edgeBuilders = new List<(string id, Func<Area> builder)>
        {
            ("clearing",       BuildClearing),
            ("slope_section",  BuildSlopeSection),
        };
        var deepBuilders = new List<(string id, Func<Area> builder)>
        {
            ("thicket",        BuildThicket),
            ("old_growth",     BuildOldGrowth),
            ("deadwood_patch", BuildDeadwoodPatch),
        };
        if (_hasStream)
            edgeBuilders.Add(("streamside", BuildStreamside));

        // Always include Clearing + 2-4 others
        _clearing = BuildClearing();
        var edgeAreas = new List<Area> { _clearing };
        var deepAreas = new List<Area>();

        int total = rng.Next(3, 6);
        int extras = total - 1;

        // Pick from deep first to balance
        int deepCount = Math.Min(rng.Next(1, 3), deepBuilders.Count);
        var deepIdx = SampleUniqueIndices(rng, deepBuilders.Count, deepCount);
        foreach (var idx in deepIdx)
            deepAreas.Add(deepBuilders[idx].builder());

        int remainingExtras = extras - deepCount;
        if (remainingExtras > 0)
        {
            // Skip Clearing in pool — already present
            var edgePool = edgeBuilders.Skip(1).ToList();
            int edgeCount = Math.Min(remainingExtras, edgePool.Count);
            var edgeIdx = SampleUniqueIndices(rng, edgePool.Count, edgeCount);
            foreach (var idx in edgeIdx)
                edgeAreas.Add(edgePool[idx].builder());
        }

        // ── Populate spots before registration ───────────────────────────────

        foreach (var area in edgeAreas.Concat(deepAreas))
            PopulateArea(area, rng);

        // Add camp PoIs to Clearing when woodcutter or charcoal burner present
        if (_hasWoodcutter || _hasCharcoalBurner)
        {
            foreach (var poi in CampSubfactory.BuildForestCamp())
                _clearing!.PointsOfInterest.Add(poi);
        }

        // ── Build sections ───────────────────────────────────────────────────

        var edge = new Section(
            "Forest Edge",
            new() { "Lighter canopy, more undergrowth, sky visible between trunks" }
        );
        edge.Areas.AddRange(edgeAreas);
        scene.Sections.Add(edge);
        RegisterAll(scene, edge);

        var deep = new Section(
            "Deep Wood",
            new() { "Dense canopy, darker, quieter; the great trees stand close together" }
        );
        deep.Areas.AddRange(deepAreas);
        scene.Sections.Add(deep);
        RegisterAll(scene, deep);

        _allAreas.AddRange(edgeAreas);
        _allAreas.AddRange(deepAreas);

        // ── Connect with PathPoIs ────────────────────────────────────────────

        for (int i = 0; i < _allAreas.Count - 1; i++)
        {
            var a = _allAreas[i];
            var b = _allAreas[i + 1];
            scene.ConnectAreasBidirectional(a, b);
            string name = (a.DisplayName == "Streamside" || b.DisplayName == "Streamside")
                ? "Stream Path"
                : (a.DisplayName == "Thicket" || b.DisplayName == "Thicket") ? "Narrow Path" : "Forest Track";
            var path = new PathPointOfInterest(
                a, b, name,
                new() { $"A track winding between {a.DisplayName.ToLowerInvariant()} and {b.DisplayName.ToLowerInvariant()}" },
                new[] { "narrow", "leaf-strewn", "winding" }
            );
            a.PointsOfInterest.Add(path);
            b.PointsOfInterest.Add(path);
            path.Register(scene);
        }

        Console.WriteLine($"ForestSceneFactory: {_type} forest, {_allAreas.Count} areas, woodcutter={_hasWoodcutter}, charcoal={_hasCharcoalBurner}");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildClearing() => new(
        displayName: "Clearing",
        contextDescription: "in the forest clearing",
        transitionDescription: "step into the clearing",
        descriptions: new() { "An open patch among the trees, sunlight reaching the forest floor" },
        moods: new[] { "open", "sunlit", "quiet", "wide" }
    );

    private static Area BuildThicket() => new(
        displayName: "Thicket",
        contextDescription: "pushing through the thicket",
        transitionDescription: "force into the thicket",
        descriptions: new() { "A dense tangle of saplings, brambles, and low growth" },
        moods: new[] { "dense", "snarled", "shadowed", "low" }
    );

    private static Area BuildOldGrowth() => new(
        displayName: "Old Growth",
        contextDescription: "among the great trees of the old growth",
        transitionDescription: "step into the old growth",
        descriptions: new() { "Massive ancient trees stand close together, undergrowth sparse beneath" },
        moods: new[] { "ancient", "still", "vaulted", "dark", "spacious" }
    );

    private static Area BuildStreamside() => new(
        displayName: "Streamside",
        contextDescription: "beside a forest stream",
        transitionDescription: "follow the stream's edge",
        descriptions: new() { "A creek winds through the wood, mud-banked and clear-running" },
        moods: new[] { "wet", "cool", "running", "muddy" }
    );

    private static Area BuildDeadwoodPatch() => new(
        displayName: "Deadwood Patch",
        contextDescription: "in the deadwood patch",
        transitionDescription: "step into the deadwood patch",
        descriptions: new() { "Fallen and rotting trees lie heaped where storms felled them" },
        moods: new[] { "rotting", "fungal", "still", "tangled" }
    );

    private static Area BuildSlopeSection() => new(
        displayName: "Slope Section",
        contextDescription: "on the forested slope",
        transitionDescription: "climb the forested slope",
        descriptions: new() { "The forest climbs a hillside, roots breaking through the loose soil" },
        moods: new[] { "tilted", "uneven", "rooted", "shaded" }
    );

    // ── Spot population ──────────────────────────────────────────────────────

    private void PopulateArea(Area area, Random rng)
    {
        // 1–3 living trees per area, species by forest type
        int treeCount = rng.Next(1, 4);
        for (int i = 0; i < treeCount; i++)
            area.PointsOfInterest.Add(PickTree(rng));

        // Cut/fallen wood — concentrated in Deadwood Patch and Old Growth
        if (area.DisplayName == "Deadwood Patch" || area.DisplayName == "Old Growth")
        {
            area.PointsOfInterest.Add(TerrainSubfactory.BuildFelledLog());
            area.PointsOfInterest.Add(TerrainSubfactory.BuildTreeStump());
            area.PointsOfInterest.Add(TerrainSubfactory.BuildDeadfall());
        }
        else if (rng.NextDouble() < 0.4)
        {
            area.PointsOfInterest.Add(TerrainSubfactory.BuildFelledLog());
        }

        // Ground vegetation
        if (rng.NextDouble() < 0.6) area.PointsOfInterest.Add(TerrainSubfactory.BuildUndergrowthPatch());
        if (rng.NextDouble() < 0.4) area.PointsOfInterest.Add(TerrainSubfactory.BuildMushroomCluster());
        if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(TerrainSubfactory.BuildMossBank());

        // Streamside-specific
        if (area.DisplayName == "Streamside")
            area.PointsOfInterest.Add(TerrainSubfactory.BuildStreamBank());
    }

    private PointOfInterest PickTree(Random rng)
    {
        switch (_type)
        {
            case ForestType.Coniferous:
                return TerrainSubfactory.BuildPineTree();
            case ForestType.Mixed:
                return rng.Next(4) switch
                {
                    0 => TerrainSubfactory.BuildOakTree(),
                    1 => TerrainSubfactory.BuildBeechTree(),
                    2 => TerrainSubfactory.BuildPineTree(),
                    _ => TerrainSubfactory.BuildBirchTree(),
                };
            default: // Deciduous
                return rng.Next(4) switch
                {
                    0 => TerrainSubfactory.BuildOakTree(),
                    1 => TerrainSubfactory.BuildBeechTree(),
                    2 => TerrainSubfactory.BuildAshTree(),
                    _ => TerrainSubfactory.BuildBirchTree(),
                };
        }
    }

    // ── NPC construction ────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_clearing is null || _allAreas.Count == 0) return;

        if (_hasWoodcutter)
            SpawnNamed(rng, scene, new WoodcutterArchetype(), BuildWoodcutterSchedule());

        if (_hasCharcoalBurner)
            SpawnNamed(rng, scene, new CharcoalBurnerArchetype(), BuildWoodcutterSchedule());

        // Beasts
        TrySpawnNamed(rng, scene, new BoarArchetype(), 0.40);
        TrySpawnNamed(rng, scene, new WolfArchetype(), 0.20);

        // Shallow wildlife
        TrySpawnShallow(rng, scene, new DeerArchetype(),       0.50);
        TrySpawnShallow(rng, scene, new BadgerArchetype(),     0.30);
        TrySpawnShallow(rng, scene, new SquirrelArchetype(),   0.60);
        TrySpawnShallow(rng, scene, new WoodMouseArchetype(),  0.45);
        TrySpawnShallow(rng, scene, new RobinArchetype(),      0.50);
        TrySpawnShallow(rng, scene, new WoodpeckerArchetype(), 0.30);
        TrySpawnShallow(rng, scene, new OwlArchetype(),        0.30);
    }

    private NpcSchedule BuildWoodcutterSchedule()
    {
        var clearing = _clearing!.DisplayName.ToLowerInvariant();
        // Pick deep-wood area for morning; fall back to clearing if unavailable
        var workArea = _allAreas.FirstOrDefault(a => a.DisplayName == "Old Growth" || a.DisplayName == "Thicket");
        var workId   = workArea?.DisplayName.ToLowerInvariant() ?? clearing;
        var deadwood = _allAreas.FirstOrDefault(a => a.DisplayName == "Deadwood Patch");
        var hauleId  = deadwood?.DisplayName.ToLowerInvariant() ?? clearing;

        return NpcSchedule.Roaming(new()
        {
            [TimePeriod.Dawn]      = clearing,
            [TimePeriod.Morning]   = workId,
            [TimePeriod.Noon]      = clearing,
            [TimePeriod.Afternoon] = hauleId,
            [TimePeriod.Evening]   = clearing,
            [TimePeriod.Night]     = clearing,
        });
    }

    private void SpawnNamed(Random rng, Scene scene, NamedNpcArchetype archetype, NpcSchedule schedule)
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
        var entity = archetype.Spawn(rng, _clearing!.ContextDescription, saved);
        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = schedule;
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

    private void TrySpawnShallow(Random rng, Scene scene, ShallowNpcArchetype archetype, double chance)
    {
        if (rng.NextDouble() > chance) return;
        var area = _allAreas[rng.Next(_allAreas.Count)];
        var entity = archetype.Spawn(rng, area.DisplayName.ToLowerInvariant());
        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = NpcSchedule.Always(area.DisplayName.ToLowerInvariant());
    }
}
