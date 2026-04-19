using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Scene.Plain;

/// <summary>
/// Builds a procedural plain scene.
///
/// Structure:
///   Section "Flatlands" → Grassland, Meadow areas  (if sampled)
///   Section "Highlands" → Hill, Valley areas        (if sampled)
///   2-4 areas sampled, connected linearly with bidirectional edges.
///   Each area gets 1-3 spots (apple tree, boulder, bush, flower patch, pine tree).
///
/// NPCs:
///   Fox (dawn/dusk), Stray Dog (always), Stray Cat (morning/evening), Black Bear (daytime)
/// </summary>
public class PlainSceneFactory : SceneFactory
{
    public PlainSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // Step 1: Sample 2-4 areas
        var allAreaBuilders = new List<Func<Area>>
        {
            () => BuildGrassland(),
            () => BuildMeadow(),
            () => BuildHill(),
            () => BuildValley(),
        };
        // indices 0,1 = flatlands, 2,3 = highlands
        var flatlandIds  = new HashSet<int> { 0, 1 };
        var highlandIds  = new HashSet<int> { 2, 3 };

        int areaCount = rng.Next(2, 5); // 2-4
        var sampledIndices = SampleUniqueIndices(rng, allAreaBuilders.Count, areaCount);

        Console.WriteLine($"PlainSceneFactory: Sampled {areaCount} areas");

        var flatlandAreas  = new List<Area>();
        var highlandAreas  = new List<Area>();

        foreach (var idx in sampledIndices)
        {
            var area = allAreaBuilders[idx]();
            if (flatlandIds.Contains(idx))
                flatlandAreas.Add(area);
            else
                highlandAreas.Add(area);
        }

        // Build sections (only if they have areas)
        if (flatlandAreas.Count > 0)
        {
            var flatlands = new Section("Flatlands", new() { "Open, low-lying terrain" });
            flatlands.Areas.AddRange(flatlandAreas);
            scene.Sections.Add(flatlands);
            RegisterAll(scene, flatlands);
        }

        if (highlandAreas.Count > 0)
        {
            var highlands = new Section("Highlands", new() { "Elevated, exposed terrain" });
            highlands.Areas.AddRange(highlandAreas);
            scene.Sections.Add(highlands);
            RegisterAll(scene, highlands);
        }

        // Step 2: Connect areas linearly (bidirectional) in sampled order
        var allAreas = scene.AllAreas;
        for (int i = 0; i < allAreas.Count - 1; i++)
            scene.ConnectAreasBidirectional(allAreas[i], allAreas[i + 1]);

        // Step 3: Add 1-3 points of interest per area
        var poiBuilders = new List<Func<PointOfInterest>>
        {
            () => BuildAppleTreePointOfInterest(),
            () => BuildBoulderPointOfInterest(),
            () => BuildBushPointOfInterest(),
            () => BuildFlowerPatchPointOfInterest(),
            () => BuildPineTreePointOfInterest(),
        };

        foreach (var area in allAreas)
        {
            int poiCount = rng.Next(1, 4);
            var poiIndices = SampleUniqueIndices(rng, poiBuilders.Count, poiCount);
            foreach (var idx in poiIndices)
            {
                var poi = poiBuilders[idx]();
                area.PointsOfInterest.Add(poi);
                poi.Register(scene);
                foreach (var itemEl in poi.Items)
                    itemEl.Register(scene);
            }
        }

        Console.WriteLine($"PlainSceneFactory: Built plain scene (entry '{allAreas.FirstOrDefault()?.DisplayName}')");
    }

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        var encounters = new List<(NamedNpcArchetype Archetype, float SpawnChance)>
        {
            (new FoxArchetype(),       0.40f),
            (new StrayDogArchetype(),  0.30f),
            (new StrayCatArchetype(),  0.25f),
            (new BlackBearArchetype(), 0.15f),
        };

        var allAreas = scene.AllAreas;
        if (allAreas.Count == 0) return;

        foreach (var (archetype, spawnChance) in encounters)
        {
            if (rng.NextDouble() > spawnChance) continue;

            var targetArea = allAreas[rng.Next(allAreas.Count)];
            var entity = archetype.Spawn(rng, targetArea.ContextDescription);
            var sceneNpc = new SceneNpc(entity);
            sceneNpc.Register(scene);
            scene.Npcs.Add(sceneNpc);

            var schedule = BuildPlainSchedule(archetype.ArchetypeId, allAreas, rng);
            scene.NpcSchedules[sceneNpc.Id] = schedule;
        }
    }

    private static NpcSchedule BuildPlainSchedule(string archetypeId, List<Area> areas, Random rng)
    {
        // Which periods the NPC can appear at all (archetype-dependent)
        TimePeriod[] activePeriods = archetypeId switch
        {
            "fox"        => new[] { TimePeriod.Dawn, TimePeriod.Evening },
            "black_bear" => new[] { TimePeriod.Morning, TimePeriod.Noon, TimePeriod.Afternoon },
            "stray_cat"  => new[] { TimePeriod.Morning, TimePeriod.Evening },
            _            => (TimePeriod[])Enum.GetValues(typeof(TimePeriod)),
        };

        var map = new Dictionary<TimePeriod, string?>();
        foreach (TimePeriod p in Enum.GetValues(typeof(TimePeriod)))
        {
            if (Array.IndexOf(activePeriods, p) < 0)
            {
                map[p] = null; // absent this period
                continue;
            }
            // 25% chance absent even during an active period
            if (rng.NextDouble() < 0.25)
            {
                map[p] = null;
                continue;
            }
            map[p] = areas[rng.Next(areas.Count)].DisplayName.ToLowerInvariant();
        }

        // Ensure at least one active period so the NPC isn't completely invisible
        if (map.Values.All(v => v == null))
        {
            var period = activePeriods[rng.Next(activePeriods.Length)];
            map[period] = areas[rng.Next(areas.Count)].DisplayName.ToLowerInvariant();
        }

        return NpcSchedule.Roaming(map);
    }

    // ── Area builders ─────────────────────────────────────────────────────────

    private static Area BuildGrassland() => new(
        displayName: "Grassland",
        contextDescription: "crossing the open grassland",
        transitionDescription: "move into the grassland",
        descriptions: new() { "Vast flat terrain of tall grass, sparse trees, wide sky" },
        moods: new[] { "vast", "flat", "dry", "sweeping", "yellowed", "rustling", "endless", "sparse" }
    );

    private static Area BuildMeadow() => new(
        displayName: "Meadow",
        contextDescription: "wandering through the open meadow",
        transitionDescription: "move into the open meadow",
        descriptions: new() { "An open grassy expanse dotted with wildflowers, gentle and exposed" },
        moods: new[] { "sunlit", "breezy", "quiet", "open", "peaceful", "windswept", "golden", "wide" }
    );

    private static Area BuildHill() => new(
        displayName: "Hill",
        contextDescription: "climbing the open hill",
        transitionDescription: "move up onto the hill",
        descriptions: new() { "A gentle rise in the plain, offering a wider view, exposed to wind" },
        moods: new[] { "windswept", "exposed", "grassy", "bare", "lonely", "rolling", "open", "bleak" }
    );

    private static Area BuildValley() => new(
        displayName: "Valley",
        contextDescription: "descending into the shallow valley",
        transitionDescription: "descend into the valley",
        descriptions: new() { "A shallow depression sheltered from wind, damp ground, lush growth" },
        moods: new[] { "sheltered", "damp", "quiet", "lush", "shadowed", "still", "secluded", "overgrown" }
    );

    // ── PointOfInterest builders ──────────────────────────────────────────────

    private static PointOfInterest BuildAppleTreePointOfInterest() => new(
        displayName: "Apple Tree",
        descriptions: new() { "A gnarled apple tree standing alone" },
        items: new()
        {
            new ItemElement(new AppleLeaf()),
            new ItemElement(new Branch()),
            new ItemElement(new Apple()),
            new ItemElement(new Bark()),
        },
        moods: new[] { "gnarled", "laden", "solitary", "weathered", "ancient", "spreading", "crooked", "still" }
    );

    private static PointOfInterest BuildBoulderPointOfInterest() => new(
        displayName: "Boulder",
        descriptions: new() { "A large stone half-buried in the ground" },
        items: new()
        {
            new ItemElement(new Rock()),
            new ItemElement(new Moss()),
            new ItemElement(new Mushroom()),
        },
        moods: new[] { "grey", "weathered", "mossy", "cold", "ancient", "massive", "silent", "half-buried" }
    );

    private static PointOfInterest BuildBushPointOfInterest() => new(
        displayName: "Bush",
        descriptions: new() { "A thorny shrub common across the plain" },
        items: new()
        {
            new ItemElement(new BushLeaf()),
            new ItemElement(new Thorn()),
            new ItemElement(new WildBerry()),
        },
        moods: new[] { "thorny", "dense", "tangled", "dark", "overgrown", "low", "wild", "scraggly" }
    );

    private static PointOfInterest BuildFlowerPatchPointOfInterest() => new(
        displayName: "Flower Patch",
        descriptions: new() { "A colourful patch of wildflowers" },
        items: new()
        {
            new ItemElement(new Daisy()),
            new ItemElement(new Poppy()),
            new ItemElement(new Clover()),
            new ItemElement(new Dandelion()),
        },
        moods: new[] { "bright", "fragrant", "colourful", "quiet", "cheerful", "scattered", "wild", "vivid" }
    );

    private static PointOfInterest BuildPineTreePointOfInterest() => new(
        displayName: "Pine Tree",
        descriptions: new() { "A lone pine standing at the edge of the plain" },
        items: new()
        {
            new ItemElement(new Branch()),
            new ItemElement(new Bark()),
            new ItemElement(new PineSap()),
            new ItemElement(new PineCone()),
            new ItemElement(new PineNeedle()),
        },
        moods: new[] { "tall", "solitary", "resinous", "dark", "wind-bent", "dense", "towering", "scraggly" }
    );
}
