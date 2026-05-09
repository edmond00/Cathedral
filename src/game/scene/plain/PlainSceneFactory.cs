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

namespace Cathedral.Game.Scene.Plain;

/// <summary>
/// Builds a procedural plain scene per the v1 world-content spec (plain.md).
///
/// Identity: open vs heath vs wetland (drives section pool, area pool, tree species).
///   • open    → Flatlands + Highlands; oak / hawthorn trees
///   • heath   → Flatlands + Highlands; elder / hawthorn trees
///   • wetland → Flatlands + Wetlands; willow / elder trees, reed beds
///
/// Areas: 3–5 sampled from { Grassland, Meadow, Heath, Hill, Valley, Hedgerow, Boggy Ground }.
/// Connections: <see cref="PathPointOfInterest"/> between adjacent areas.
/// Plain is uninhabited terrain — only beasts and shallow wildlife spawn.
/// </summary>
public class PlainSceneFactory : SceneFactory
{
    public PlainSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum Identity { Open, Heath, Wetland }

    private Identity _identity;
    private readonly List<Area> _allOutdoorAreas = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // ── 1. Roll identity ──────────────────────────────────────────────────

        var roll = rng.NextDouble();
        _identity = roll switch
        {
            < 0.25 => Identity.Wetland,
            < 0.55 => Identity.Heath,
            _      => Identity.Open,
        };

        // ── 2. Choose area pool & sample 3–5 areas ────────────────────────────

        var (flatBuilders, highBuilders) = BuildAreaPools(_identity);
        int areaCount = rng.Next(3, 6);

        int flatTake = Math.Min(areaCount / 2 + 1, flatBuilders.Count);
        var flatAreas = SampleFromPool(rng, flatBuilders, flatTake);
        int remaining = areaCount - flatAreas.Count;
        var highAreas = SampleFromPool(rng, highBuilders, Math.Min(remaining, highBuilders.Count));

        // ── 3. Populate spots in each area BEFORE registration ────────────────

        foreach (var area in flatAreas.Concat(highAreas))
            PopulateAreaSpots(area, rng);

        // ── 4. Build & register sections ─────────────────────────────────────

        var flatSection = new Section(
            "Flatlands",
            new() { "Open, low-lying ground; wide sky and long sightlines" }
        );
        flatSection.Areas.AddRange(flatAreas);
        scene.Sections.Add(flatSection);
        RegisterAll(scene, flatSection);

        var highSectionName = _identity == Identity.Wetland ? "Wetlands" : "Highlands";
        var highSectionDesc = _identity == Identity.Wetland
            ? "Boggy ground, reeds, willows, soft underfoot"
            : "Gentle rises and shallow valleys";
        var highSection = new Section(highSectionName, new() { highSectionDesc });
        highSection.Areas.AddRange(highAreas);
        scene.Sections.Add(highSection);
        RegisterAll(scene, highSection);

        _allOutdoorAreas.AddRange(flatAreas);
        _allOutdoorAreas.AddRange(highAreas);

        // ── 5. Connect areas linearly with PathPoIs (register PoI explicitly) ─

        for (int i = 0; i < _allOutdoorAreas.Count - 1; i++)
        {
            var a = _allOutdoorAreas[i];
            var b = _allOutdoorAreas[i + 1];
            scene.ConnectAreasBidirectional(a, b);
            var path = BuildPath(a, b, rng);
            a.PointsOfInterest.Add(path);
            b.PointsOfInterest.Add(path);
            path.Register(scene);
        }

        Console.WriteLine($"PlainSceneFactory: Built '{_identity}' plain — {_allOutdoorAreas.Count} areas");
    }

    // ── Area-pool selection ──────────────────────────────────────────────────

    private (List<(string id, Func<Area> builder)> flat, List<(string id, Func<Area> builder)> high)
        BuildAreaPools(Identity identity)
    {
        var flat = new List<(string, Func<Area>)>
        {
            ("grassland", BuildGrassland),
            ("meadow",    BuildMeadow),
            ("hedgerow",  BuildHedgerow),
        };
        var high = new List<(string, Func<Area>)>
        {
            ("hill",   BuildHill),
            ("valley", BuildValley),
        };

        if (identity == Identity.Heath)
            flat.Add(("heath", BuildHeath));

        if (identity == Identity.Wetland)
        {
            high.Clear();
            high.Add(("boggy_ground", BuildBoggyGround));
            high.Add(("valley",       BuildValley));
        }

        return (flat, high);
    }

    private static List<Area> SampleFromPool(Random rng, List<(string id, Func<Area> builder)> pool, int count)
    {
        if (count <= 0 || pool.Count == 0) return new List<Area>();
        var indices = SampleUniqueIndices(rng, pool.Count, count);
        return indices.Select(i => pool[i].builder()).ToList();
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildGrassland() => new(
        displayName: "Grassland",
        contextDescription: "crossing the open grassland",
        transitionDescription: "move into the grassland",
        descriptions: new() { "Open wind-swept grassland, the sky vast and unbroken above" },
        moods: new[] { "vast", "windy", "yellowed", "rustling", "open" }
    );

    private static Area BuildMeadow() => new(
        displayName: "Meadow",
        contextDescription: "wandering through the meadow",
        transitionDescription: "move into the meadow",
        descriptions: new() { "A sheltered meadow speckled with wildflowers and the hum of bees" },
        moods: new[] { "sunlit", "fragrant", "soft", "quiet", "windless" }
    );

    private static Area BuildHeath() => new(
        displayName: "Heath",
        contextDescription: "crossing the dry heath",
        transitionDescription: "move into the heath",
        descriptions: new() { "A dry stretch of scrub and heather, broken by stands of gorse" },
        moods: new[] { "dry", "scrubby", "purple", "wind-bent", "low" }
    );

    private static Area BuildHill() => new(
        displayName: "Hill",
        contextDescription: "climbing the open hill",
        transitionDescription: "move up onto the hill",
        descriptions: new() { "A gentle rise of grass and stone, exposed to the wind" },
        moods: new[] { "windswept", "exposed", "rolling", "bare", "open" }
    );

    private static Area BuildValley() => new(
        displayName: "Valley",
        contextDescription: "descending into the valley",
        transitionDescription: "descend into the valley",
        descriptions: new() { "A shallow damp depression sheltered from the wind" },
        moods: new[] { "sheltered", "damp", "lush", "shadowed", "still" }
    );

    private static Area BuildHedgerow() => new(
        displayName: "Hedgerow",
        contextDescription: "walking along the hedgerow",
        transitionDescription: "move along the hedgerow",
        descriptions: new() { "A long thorny hedgerow standing between two open stretches" },
        moods: new[] { "tangled", "thorny", "dense", "narrow", "shaded" }
    );

    private static Area BuildBoggyGround() => new(
        displayName: "Boggy Ground",
        contextDescription: "wading through boggy ground",
        transitionDescription: "step into the boggy ground",
        descriptions: new() { "Soft wet ground beneath a tangle of reeds and rushes" },
        moods: new[] { "wet", "soft", "still", "rustling", "treacherous" }
    );

    // ── Spot population (called BEFORE area is registered) ───────────────────

    private void PopulateAreaSpots(Area area, Random rng)
    {
        // 1–2 trees per area
        int treeCount = rng.Next(1, 3);
        for (int i = 0; i < treeCount; i++)
            area.PointsOfInterest.Add(PickTreeForArea(rng));

        if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(TerrainSubfactory.BuildBoulder());
        if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(TerrainSubfactory.BuildFlowerPatch());
        if (rng.NextDouble() < 0.4) area.PointsOfInterest.Add(PickBerryBush(rng));
        if (rng.NextDouble() < 0.3) area.PointsOfInterest.Add(TerrainSubfactory.BuildMushroomCluster());

        if (_identity == Identity.Wetland && area.DisplayName == "Boggy Ground")
            area.PointsOfInterest.Add(TerrainSubfactory.BuildReedBed());
    }

    private PointOfInterest PickTreeForArea(Random rng)
    {
        switch (_identity)
        {
            case Identity.Open:    return rng.NextDouble() < 0.5 ? TerrainSubfactory.BuildOakTree()    : TerrainSubfactory.BuildHawthornTree();
            case Identity.Heath:   return rng.NextDouble() < 0.5 ? TerrainSubfactory.BuildElderTree()  : TerrainSubfactory.BuildHawthornTree();
            case Identity.Wetland: return rng.NextDouble() < 0.6 ? TerrainSubfactory.BuildWillowTree() : TerrainSubfactory.BuildElderTree();
            default:               return TerrainSubfactory.BuildOakTree();
        }
    }

    private static PointOfInterest PickBerryBush(Random rng) => rng.Next(3) switch
    {
        0 => TerrainSubfactory.BuildBerryBush(),
        1 => TerrainSubfactory.BuildBilberryBush(),
        _ => TerrainSubfactory.BuildSloeBush(),
    };

    private static PathPointOfInterest BuildPath(Area a, Area b, Random rng)
    {
        string name = (a.DisplayName, b.DisplayName) switch
        {
            ("Heath", _)        or (_, "Heath")        => "Heath Track",
            ("Boggy Ground", _) or (_, "Boggy Ground") => "Bog Track",
            ("Hedgerow", _)     or (_, "Hedgerow")     => "Hedge Gap",
            _ => "Open Path",
        };
        return new PathPointOfInterest(
            areaA: a,
            areaB: b,
            displayName: name,
            descriptions: new() { $"A worn track running between {a.DisplayName.ToLowerInvariant()} and {b.DisplayName.ToLowerInvariant()}" },
            moods: new[] { "worn", "narrow", "winding" }
        );
    }

    // ── NPC construction ────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allOutdoorAreas.Count == 0) return;

        TrySpawnNamed(rng, scene, new BoarArchetype(), 0.25);
        TrySpawnNamed(rng, scene, new FoxArchetype(),  0.40);
        TrySpawnNamed(rng, scene, new WolfArchetype(), 0.10);

        TrySpawnShallow(rng, scene, new DeerArchetype(),    0.50);
        TrySpawnShallow(rng, scene, new HareArchetype(),    0.50);
        TrySpawnShallow(rng, scene, new CrowArchetype(),    0.60);
        TrySpawnShallow(rng, scene, new LarkArchetype(),    0.45);
        TrySpawnShallow(rng, scene, new SparrowArchetype(), 0.45);

        if (_identity == Identity.Wetland)
        {
            TrySpawnShallow(rng, scene, new FrogArchetype(), 0.6);
            TrySpawnShallow(rng, scene, new ToadArchetype(), 0.4);
        }
    }

    private void TrySpawnNamed(Random rng, Scene scene, NamedNpcArchetype archetype, double chance)
    {
        if (rng.NextDouble() > chance) return;
        var area = _allOutdoorAreas[rng.Next(_allOutdoorAreas.Count)];
        var entity = archetype.Spawn(rng, area.ContextDescription);
        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = NpcSchedule.Always(area.DisplayName.ToLowerInvariant());
    }

    private void TrySpawnShallow(Random rng, Scene scene, ShallowNpcArchetype archetype, double chance)
    {
        if (rng.NextDouble() > chance) return;
        var area = _allOutdoorAreas[rng.Next(_allOutdoorAreas.Count)];
        var entity = archetype.Spawn(rng, area.DisplayName.ToLowerInvariant());
        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = NpcSchedule.Always(area.DisplayName.ToLowerInvariant());
    }
}
