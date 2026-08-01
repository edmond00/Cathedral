using System.Collections.Generic;
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
        MergeDuplicateNamedPois(scene);
        AssignStableKeys(scene);
        AssignDepletionKeys(scene);
        WriteSceneToLog(scene, locationId);

        return scene;
    }

    /// <summary>
    /// Collapses same-named points of interest within an area into one, moving the losers' items onto
    /// the survivor.
    ///
    /// <para>The observation phase de-duplicates its candidates by display name, so a second PoI
    /// called "Oak Tree" in the same area could never be observed — and anything inside it could never
    /// be reached. Terrain builders sample with replacement and produced these routinely: an orchard
    /// with two apple trees showed one, and half its fruit was unreachable. Merging rather than
    /// dropping keeps the yield: the surviving tree becomes the stand.</para>
    ///
    /// <para>Runs after <see cref="BuildSections"/> so it catches every factory, and before
    /// <see cref="AssignStableKeys"/> so keys are assigned to the list that actually survives.</para>
    /// </summary>
    private static void MergeDuplicateNamedPois(Scene scene)
    {
        foreach (var area in scene.AllAreas)
        {
            Merge(area.PointsOfInterest);
            foreach (var spot in area.Spots)
                Merge(spot.PointsOfInterest);
        }

        static void Merge(List<PointOfInterest> pois)
        {
            var byName = new Dictionary<string, PointOfInterest>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < pois.Count; i++)
            {
                var poi = pois[i];
                if (byName.TryAdd(poi.DisplayName, poi)) continue;

                // A connector is legitimately listed once in each of the two areas it joins, but
                // never twice in the same one — so anything reaching here is a genuine duplicate.
                byName[poi.DisplayName].Items.AddRange(poi.Items);
                pois.RemoveAt(i--);
            }
        }
    }

    /// <summary>
    /// Assigns every element a rebuild-independent <see cref="Element.StableKey"/> in deterministic
    /// build order. Keys are built from ordinal positions rather than display names, so two rooms
    /// called "Hall" in different buildings — or two "Straw Pallet" PoIs in one room — never collide.
    ///
    /// <para>Elements that already carry a key are skipped. Connectors (doors, stairs, paths) appear
    /// in two areas' PoI lists, so the walk would visit them twice and the key would end up depending
    /// on which side happened to be walked last; <c>BuildingFactory</c> keys those at construction
    /// instead.</para>
    ///
    /// <para>Deliberately separate from <see cref="AssignDepletionKeys"/>: that key is matched against
    /// persisted <c>LocationInstanceState.ItemDepletions</c> data, so its format must not move.</para>
    /// </summary>
    private static void AssignStableKeys(Scene scene)
    {
        for (int si = 0; si < scene.Sections.Count; si++)
        {
            var section = scene.Sections[si];
            var sectionKey = $"s{si}:{section.DisplayName}";
            if (section.StableKey.Length == 0) section.StableKey = sectionKey;

            for (int ai = 0; ai < section.Areas.Count; ai++)
            {
                var area = section.Areas[ai];
                var areaKey = $"{sectionKey}|a{ai}:{area.DisplayName}";
                if (area.StableKey.Length == 0) area.StableKey = areaKey;

                KeyPois(areaKey, area.PointsOfInterest);

                for (int spi = 0; spi < area.Spots.Count; spi++)
                {
                    var spot = area.Spots[spi];
                    var spotKey = $"{areaKey}|sp{spi}:{spot.ReferenceLemma}";
                    if (spot.StableKey.Length == 0) spot.StableKey = spotKey;
                    KeyPois(spotKey, spot.PointsOfInterest);
                }
            }
        }

        static void KeyPois(string parentKey, List<PointOfInterest> pois)
        {
            for (int pi = 0; pi < pois.Count; pi++)
            {
                var poi = pois[pi];
                var poiKey = $"{parentKey}|p{pi}:{poi.ReferenceLemma}";
                if (poi.StableKey.Length == 0) poi.StableKey = poiKey;

                for (int ii = 0; ii < poi.Items.Count; ii++)
                    if (poi.Items[ii].StableKey.Length == 0)
                        poi.Items[ii].StableKey = $"{poi.StableKey}|i{ii}";
            }
        }
    }

    /// <summary>
    /// Assigns each item a stable <see cref="ItemElement.DepletionKey"/> in deterministic build order
    /// (areas → their spots → PoIs → items). Because factories are seeded by locationId, the same
    /// physical slot maps to the same key on every rebuild, so depletion/regeneration can be tracked
    /// across visits without persisting element GUIDs.
    /// </summary>
    private static void AssignDepletionKeys(Scene scene)
    {
        foreach (var area in scene.AllAreas)
        {
            KeyContainer(area.DisplayName, "", area.PointsOfInterest);
            foreach (var spot in area.Spots)
                KeyContainer(area.DisplayName, spot.ReferenceLemma, spot.PointsOfInterest);
        }

        static void KeyContainer(string areaName, string spotName, List<PointOfInterest> pois)
        {
            var lemmaCounts = new Dictionary<string, int>();
            foreach (var poi in pois)
            {
                lemmaCounts.TryGetValue(poi.ReferenceLemma, out int ordinal);
                lemmaCounts[poi.ReferenceLemma] = ordinal + 1;
                for (int i = 0; i < poi.Items.Count; i++)
                    poi.Items[i].DepletionKey = $"{areaName}|{spotName}|{poi.ReferenceLemma}#{ordinal}|{i}";
            }
        }
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

    /// <summary>
    /// Adds a generated building to the scene: its single section, every room in it, and the entry
    /// door.
    ///
    /// <para>The entry door is registered separately because it straddles the boundary — it lives in
    /// both the outdoor area's PoI list and the hall's, and the outdoor area belongs to a different
    /// section that was registered earlier.</para>
    ///
    /// <para>Deliberately no <c>ConnectAreas</c> call: a building's rooms are joined by doors and
    /// stairs only. An area-graph edge would give <c>MoveToAreaVerb</c> a way straight past a locked
    /// door.</para>
    /// </summary>
    protected void RegisterBuilding(Scene scene, Building.BuildingResult building)
    {
        scene.Sections.Add(building.Section);
        RegisterAll(scene, building.Section);
        building.EntryDoor.Register(scene);
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
                    foreach (var (period, area) in schedule.ActivePeriods)
                        writer.WriteLine($"  {period}: {area.DisplayName}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SceneFactory: Failed to write scene log: {ex.Message}");
        }
    }
}
