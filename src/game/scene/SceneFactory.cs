using System.IO;
using System.Linq;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Scene;

/// <summary>
/// Abstract base for scene factories. Each concrete factory (one per biome/location type)
/// takes a location ID and builds a complete <see cref="Scene"/>.
/// Deterministic: same locationId always produces the same scene structure.
/// </summary>
public abstract class SceneFactory
{
    protected readonly string? _sessionPath;

    /// <summary>
    /// Optional location state injected via <see cref="Build(int, LocationInstanceState?)"/>.
    /// Available to subclasses inside <see cref="BuildNpcs"/> for affinity injection.
    /// </summary>
    protected LocationInstanceState? _locationState;

    protected SceneFactory(string? sessionPath = null)
    {
        _sessionPath = sessionPath;
    }

    /// <summary>
    /// Builds and returns a complete <see cref="Scene"/> for the given location.
    /// </summary>
    public Scene Build(int locationId)
    {
        var rng   = CreateSeededRandom(locationId);
        var scene = new Scene();

        BuildSections(rng, locationId, scene);
        BuildNpcs(rng, locationId, scene);
        AssignVerbs(scene);
        WriteSceneToLog(scene, locationId);

        return scene;
    }

    /// <summary>
    /// Builds a scene, injecting <paramref name="locationState"/> so that subclasses can
    /// restore per-NPC affinity data when spawning named NPCs.
    /// </summary>
    public Scene Build(int locationId, LocationInstanceState? locationState)
    {
        _locationState = locationState;
        return Build(locationId);
    }

    /// <summary>
    /// Build sections, areas, spots, items and register them all in the scene.
    /// Must populate scene.Sections and scene.AreaGraph.
    /// </summary>
    protected abstract void BuildSections(Random rng, int locationId, Scene scene);

    /// <summary>
    /// Build NPCs with schedules and register them in the scene.
    /// Override to add location-specific NPCs. Default does nothing.
    /// </summary>
    protected virtual void BuildNpcs(Random rng, int locationId, Scene scene) { }

    /// <summary>
    /// Assigns verbs from the global <see cref="VerbRegistry"/> to the scene.
    /// Override to filter or add scene-specific verbs. Default adds all registered verbs.
    /// </summary>
    protected virtual void AssignVerbs(Scene scene)
    {
        scene.Verbs.AddRange(VerbRegistry.Instance.GetAll());
    }

    /// <summary>Creates a deterministic Random seeded by locationId.</summary>
    protected Random CreateSeededRandom(int locationId) => new(locationId);

    /// <summary>
    /// Registers an element and all its children (section→areas→spots/PoIs→items) in a scene.
    /// </summary>
    protected void RegisterAll(Scene scene, Section section)
    {
        section.Register(scene);
        foreach (var area in section.Areas)
            RegisterAll(scene, area);
    }

    /// <summary>
    /// Registers an area and all its PoIs, Spots (and their PoIs), and items.
    /// Call this when adding a spot or area outside the normal section hierarchy.
    /// </summary>
    protected void RegisterAll(Scene scene, Area area)
    {
        area.Register(scene);
        foreach (var poi in area.PointsOfInterest)
            RegisterPoI(scene, poi);
        foreach (var spot in area.Spots)
            RegisterAll(scene, spot);
    }

    /// <summary>Registers a spot and all its PoIs and items.</summary>
    protected void RegisterAll(Scene scene, Spot spot)
    {
        spot.Register(scene);
        foreach (var poi in spot.PointsOfInterest)
            RegisterPoI(scene, poi);
    }

    private static void RegisterPoI(Scene scene, PointOfInterest poi)
    {
        poi.Register(scene);
        foreach (var itemElement in poi.Items)
            itemElement.Register(scene);
    }

    /// <summary>Samples <paramref name="count"/> unique indices from [0, <paramref name="total"/>).</summary>
    protected static int[] SampleUniqueIndices(Random rng, int total, int count)
    {
        count = Math.Min(count, total);
        var indices = Enumerable.Range(0, total).ToList();
        var result = new int[count];
        for (int i = 0; i < count; i++)
        {
            int pick = rng.Next(indices.Count);
            result[i] = indices[pick];
            indices.RemoveAt(pick);
        }
        return result;
    }

    /// <summary>Writes scene structure to log file for debugging.</summary>
    protected void WriteSceneToLog(Scene scene, int locationId)
    {
        if (_sessionPath == null) return;

        try
        {
            var path = Path.Combine(_sessionPath, $"scene_location_{locationId}.txt");
            using var writer = new StreamWriter(path);
            writer.WriteLine($"Scene for location {locationId}");
            writer.WriteLine($"Sections: {scene.Sections.Count}");
            writer.WriteLine($"NPCs: {scene.Npcs.Count}");
            writer.WriteLine($"Verbs: {scene.Verbs.Count}");
            writer.WriteLine($"Elements: {scene.Elements.Count}");
            writer.WriteLine();

            foreach (var section in scene.Sections)
            {
                writer.WriteLine($"[Section] {section.DisplayName}");
                foreach (var area in section.Areas)
                {
                    writer.WriteLine($"  [Area] {area.DisplayName} ({area.ContextDescription})");
                    var reachable = scene.GetReachableAreas(area);
                    if (reachable.Count > 0)
                        writer.WriteLine($"    → Connects to: {string.Join(", ", reachable.Select(a => a.DisplayName))}");

                    foreach (var poi in area.PointsOfInterest)
                    {
                        writer.WriteLine($"    [PointOfInterest] {poi.DisplayName}");
                        foreach (var item in poi.Items)
                            writer.WriteLine($"      [Item] {item.DisplayName}");
                    }
                }
            }

            foreach (var npc in scene.Npcs)
            {
                writer.WriteLine($"\n[NPC] {npc.DisplayName}");
                if (scene.NpcSchedules.TryGetValue(npc.Id, out var schedule))
                {
                    foreach (var (period, nodeId) in schedule.ActivePeriods)
                        writer.WriteLine($"  {period}: {nodeId}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SceneFactory: Failed to write scene log: {ex.Message}");
        }
    }
}
