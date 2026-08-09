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

        // Hand the location's persistent stores to the scene before anything is built: the departed
        // list is read a few lines down, and depletion is read as items are placed.
        _locationState?.AttachTo(scene);

        BuildSections(rng, locationId, scene);
        BuildNpcs(rng, locationId, scene);
        DropDepartedNpcs(scene);
        PlaceBeastSign(scene);
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
            Merge(area.PointsOfInterest);

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
    /// (areas → PoIs → items). Because factories are seeded by locationId, the same physical slot maps
    /// to the same key on every rebuild, so depletion/regeneration can be tracked across visits
    /// without persisting element GUIDs.
    ///
    /// <para>The empty segment after the area name is a scar, and it stays: keys are matched against
    /// persisted <c>LocationInstanceState.ItemDepletions</c> data, and dropping it would orphan every
    /// depletion already recorded in a save. It once held the name of the enclosing spot, which no
    /// factory-built PoI ever had — so every key in existence has it empty.</para>
    /// </summary>
    private static void AssignDepletionKeys(Scene scene)
    {
        foreach (var area in scene.AllAreas)
        {
            var lemmaCounts = new Dictionary<string, int>();
            foreach (var poi in area.PointsOfInterest)
            {
                lemmaCounts.TryGetValue(poi.ReferenceLemma, out int ordinal);
                lemmaCounts[poi.ReferenceLemma] = ordinal + 1;
                for (int i = 0; i < poi.Items.Count; i++)
                    poi.Items[i].DepletionKey = $"{area.DisplayName}||{poi.ReferenceLemma}#{ordinal}|{i}";
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
    /// Registers an element and all its children (section→areas→PoIs→items) in a scene.
    /// </summary>
    protected void RegisterAll(Scene scene, Section section)
    {
        section.Register(scene);
        foreach (var area in section.Areas)
            RegisterAll(scene, area);
    }

    /// <summary>
    /// Registers an area and all its PoIs and their items.
    /// Call this when adding an area outside the normal section hierarchy.
    /// </summary>
    protected void RegisterAll(Scene scene, Area area)
    {
        area.Register(scene);
        foreach (var poi in area.PointsOfInterest)
            RegisterPoI(scene, poi);
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

        // The wall climb joins the outside area to the roof, so it has to be attached at BOTH ends —
        // unlike a landscape, a wall is as visible from the street as from the roof, and you climb
        // back down it. Attached here rather than in BuildingFactory, which has no scene: the same
        // populate-then-register split the entry door follows.
        building.WallClimb.AttachTo(scene);

        // The chimney or broken shutter, when this building rolled one: a roof's other reason to
        // exist, and where slip_into lives — an illegal entry that skips a locked door.
        building.RoofSlipIn?.AttachTo(scene);
    }

    private static void RegisterPoI(Scene scene, PointOfInterest poi)
    {
        poi.Register(scene);
        foreach (var itemElement in poi.Items)
            itemElement.Register(scene);
    }

    /// <summary>
    /// A wandering day: the creature moves between two or three of <paramref name="range"/>, changing
    /// where it is every period or two.
    ///
    /// <para>Every beast in the game was pinned to a single area for the whole day, which made a wolf
    /// a fixed hazard rather than an animal — you either walked into its clearing or you did not, and
    /// nothing you could do changed which. A roaming schedule is what makes tracking mean anything:
    /// the sign in this clearing is worth reading precisely because the wolf is somewhere else now.</para>
    /// </summary>
    protected static Narrative.NpcSchedule RoamingSchedule(Random rng, IReadOnlyList<Area> range)
    {
        if (range.Count == 0) throw new ArgumentException("A roaming range needs at least one area.", nameof(range));
        if (range.Count == 1) return Narrative.NpcSchedule.Always(range[0]);

        // Two or three haunts, held for a stretch at a time. A creature that teleported every period
        // would be untrackable in the other direction — you would never arrive in time.
        int hauntCount = Math.Min(range.Count, rng.Next(2, 4));
        var haunts = SampleUniqueIndices(rng, range.Count, hauntCount).Select(i => range[i]).ToList();

        var map = new Dictionary<Narrative.TimePeriod, Area?>();
        int index = 0;
        foreach (Narrative.TimePeriod period in Enum.GetValues<Narrative.TimePeriod>())
        {
            map[period] = haunts[index % haunts.Count];
            if (rng.NextDouble() < 0.6) index++;
        }

        return Narrative.NpcSchedule.Roaming(map);
    }

    // ── Departed NPCs ─────────────────────────────────────────────────────────

    /// <summary>
    /// Takes out everyone this location has already lost — killed, tamed into the party, or talked
    /// into joining it (see <c>Scene.RemoveNpcFromPlay</c>, which records them).
    ///
    /// <para>Runs for every factory automatically, straight after <c>BuildNpcs</c>, so no factory has
    /// to know the rule and a new one cannot forget it. Before <see cref="PlaceBeastSign"/> on
    /// purpose: sign is placed per living beast, and a departed wolf that still left tracks would
    /// give the player a trail to follow to nothing.</para>
    ///
    /// <para>No-op when no location state was injected — which is every headless audit, so they keep
    /// seeing the full population a factory can produce.</para>
    /// </summary>
    private static void DropDepartedNpcs(Scene scene)
    {
        if (scene.DepartedNpcs.Count == 0) return;

        foreach (var npc in scene.Npcs.Where(n => scene.DepartedNpcs.Contains(n.Entity.PersistentId)).ToList())
        {
            scene.Npcs.Remove(npc);
            scene.NpcSchedules.Remove(npc.Id);
            Console.WriteLine($"SceneFactory: '{npc.Entity.DisplayName}' left this location for good — not rebuilt");
        }
    }

    // ── Beast sign ────────────────────────────────────────────────────────────

    /// <summary>
    /// Puts tracks wherever a beast goes. Runs for every factory automatically, after
    /// <c>BuildNpcs</c>, so a factory that spawns a wolf gets a trackable wolf without doing
    /// anything about it.
    ///
    /// <para>Only non-speaking creatures leave sign. A woodcutter walking the same rounds leaves as
    /// many footprints as a wolf does, but following a person is <c>stalk</c> — a different verb with
    /// a different legality — and reading a man's tracks off the ground is not the same skill.</para>
    /// </summary>
    private static void PlaceBeastSign(Scene scene)
    {
        foreach (var npc in scene.Npcs)
        {
            if (!npc.IsAlive) continue;
            if (npc.Entity is Npc.NpcEntity { CanSpeak: true }) continue;
            if (npc.Entity is Npc.ShallowNpcEntity { Archetype: Npc.ShallowNpcArchetype { IsTiny: true } }) continue;
            if (!scene.NpcSchedules.TryGetValue(npc.Id, out var schedule)) continue;

            // One sign per area it visits. An animal that never leaves one area is not worth
            // tracking — you are either in the area with it or you are not.
            var visited = schedule.ActivePeriods.Select(p => p.Area).Distinct().ToList();
            if (visited.Count < 2) continue;

            string species = npc.Entity.DisplayName.ToLowerInvariant();
            foreach (var area in visited)
            {
                string name = $"{npc.Entity.DisplayName} Tracks";
                if (area.PointsOfInterest.Any(p => p.DisplayName == name)) continue;

                var sign = new FootprintPointOfInterest(npc, name, new List<string>
                {
                    $"The prints of {(species.StartsWith('a') || species.StartsWith('e') || species.StartsWith('i') || species.StartsWith('o') || species.StartsWith('u') ? "an" : "a")} {species}, pressed into the ground and pointing away",
                });

                // Keyed here because sign is placed after the section walk would have reached it.
                sign.StableKey = $"sign|{npc.Entity.NpcId}|{area.DisplayName}";
                area.PointsOfInterest.Add(sign);
                sign.Register(scene);
            }
        }
    }

    // ── Landmarks and horizons ────────────────────────────────────────────────

    /// <summary>
    /// Hangs a <see cref="LandscapePointOfInterest"/> in <paramref name="viewpoint"/> for each of
    /// <paramref name="destinations"/> — one distant place seen from a high one, and the road to it.
    ///
    /// <para>Deliberate, not automatic. This replaced a pass that found its own high ground (the top
    /// of any cliff or scale point) and named the first area of each section as a landmark. It was
    /// tidy and it was wrong: a cave's entrance hall is the top of the mineshaft ladder, so every
    /// cave in the game reported that "the country opens out below", and the places it named from a
    /// mountain were whichever areas happened to be built first. A view over a location is a
    /// statement about that location, so a factory makes it.</para>
    ///
    /// <para>The viewpoint itself is skipped, and so is any duplicate: a road to where you are
    /// standing is not a road.</para>
    /// </summary>
    /// <summary>
    /// Hangs a landscape on every building roof in the scene, one per outdoor ground area.
    ///
    /// <para>Call at the end of a factory that builds buildings. Shared rather than copied per
    /// factory because the roof itself is: <c>BuildingFactory</c> gives every building one, so a
    /// factory that forgot this left a climbable area with nothing on it — which is exactly what
    /// happened to fields, and what <c>--verb-audit</c>'s climb check now catches.</para>
    ///
    /// <para>Roofs do not see other roofs. A view over a settlement is the ground between the
    /// buildings, not a way of crossing it without ever coming down.</para>
    /// </summary>
    protected static void AddRoofLandscapes(Scene scene)
    {
        var roofs = scene.AllAreas
            .SelectMany(a => a.PointsOfInterest)
            .OfType<Building.ScalePointOfInterest>()
            .Select(sp => sp.TopArea)
            .Distinct()
            .ToList();

        var ground = scene.OutdoorAreas.Where(a => !roofs.Any(r => r.Id == a.Id)).ToList();

        foreach (var roof in roofs)
            AddLandscapes(scene, roof, ground);
    }


    protected static void AddLandscapes(Scene scene, Area viewpoint, IEnumerable<Area> destinations)
    {
        foreach (var destination in destinations.Distinct())
        {
            if (destination.Id == viewpoint.Id) continue;

            var landscape = new LandscapePointOfInterest(
                viewpoint, destination,
                $"Distant {destination.DisplayName}",
                new List<string>
                {
                    $"The {destination.DisplayName.ToLowerInvariant()} lies out there, small with distance "
                    + "and clear enough to walk to",
                });
            landscape.AttachToViewpoint(scene);
        }
    }

    // ── Shallow creature placement ────────────────────────────────────────────

    /// <summary>
    /// Puts <paramref name="count"/> instances of a shallow archetype in <paramref name="area"/>,
    /// pinned there all day.
    ///
    /// <para>Lifted here because all seven wilderness factories had written the same six lines
    /// privately, and the two inhabited ones that never got round to it — village and field — had no
    /// small creatures at all as a result.</para>
    /// </summary>
    protected static void SpawnShallow(Random rng, Scene scene, Npc.ShallowNpcArchetype archetype,
                                       Area area, int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            var entity   = archetype.Spawn(rng, area.DisplayName.ToLowerInvariant());
            var sceneNpc = new SceneNpc(entity);
            sceneNpc.Register(scene);
            scene.Npcs.Add(sceneNpc);
            scene.NpcSchedules[sceneNpc.Id] = Narrative.NpcSchedule.Always(area);
        }
    }

    /// <summary>
    /// Rolls for a shallow creature and, if it lands, puts it in a random area from
    /// <paramref name="areas"/>.
    /// </summary>
    protected static void TrySpawnShallow(Random rng, Scene scene, Npc.ShallowNpcArchetype archetype,
                                          IReadOnlyList<Area> areas, double chance)
    {
        if (areas.Count == 0 || rng.NextDouble() > chance) return;
        SpawnShallow(rng, scene, archetype, areas[rng.Next(areas.Count)]);
    }

    /// <summary>
    /// Which small creatures belong somewhere. Used by <see cref="SprinkleSmallLife"/> so a factory
    /// asks for "the sort of small life a farmyard has" rather than naming eleven archetypes.
    /// </summary>
    protected enum SmallLife
    {
        /// <summary>Buildings and streets: moths, roaches, mice, spiders.</summary>
        Settlement,

        /// <summary>Woodland: butterflies, beetles, snails, crickets, spiders.</summary>
        Woodland,

        /// <summary>Worked ground and pasture: bees, butterflies, crickets, beetles.</summary>
        Cultivated,

        /// <summary>Water margins: dragonflies, snails, lizards.</summary>
        Waterside,

        /// <summary>Bare rock and high ground: lizards, beetles, spiders. Thin, by design.</summary>
        Barren,

        /// <summary>Underground: spiders, roaches, mice. No sun-lovers.</summary>
        Subterranean,
    }

    /// <summary>
    /// Scatters a few tiny creatures of the appropriate sort across <paramref name="areas"/>.
    ///
    /// <para>Every location gets some. A place with people in it and nothing else alive reads as a
    /// stage set, and tiny creatures are the cheapest possible fix: they carry no anatomy, no
    /// affinity and no dialogue, and they give the player something to do — catch it or crush it —
    /// in a room that would otherwise offer only furniture.</para>
    ///
    /// <para>The count is rolled, so two villages differ. The pools deliberately overlap: a spider
    /// belongs almost anywhere, and finding the same beetle in a wood and a field is correct.</para>
    /// </summary>
    protected static void SprinkleSmallLife(Random rng, Scene scene, IReadOnlyList<Area> areas,
                                            SmallLife kind, int min = 2, int max = 5)
    {
        if (areas.Count == 0) return;

        Func<Npc.ShallowNpcArchetype>[] pool = kind switch
        {
            SmallLife.Settlement => new Func<Npc.ShallowNpcArchetype>[]
            {
                () => new Npc.Archetypes.MothArchetype(),
                () => new Npc.Archetypes.CockroachArchetype(),
                () => new Npc.Archetypes.HouseMouseArchetype(),
                () => new Npc.Archetypes.GardenSpiderArchetype(),
                () => new Npc.Archetypes.ButterflyArchetype(),
            },
            SmallLife.Woodland => new Func<Npc.ShallowNpcArchetype>[]
            {
                () => new Npc.Archetypes.ButterflyArchetype(),
                () => new Npc.Archetypes.BeetleArchetype(),
                () => new Npc.Archetypes.SnailArchetype(),
                () => new Npc.Archetypes.CricketArchetype(),
                () => new Npc.Archetypes.GardenSpiderArchetype(),
                () => new Npc.Archetypes.MothArchetype(),
            },
            SmallLife.Cultivated => new Func<Npc.ShallowNpcArchetype>[]
            {
                () => new Npc.Archetypes.BeeArchetype(),
                () => new Npc.Archetypes.ButterflyArchetype(),
                () => new Npc.Archetypes.CricketArchetype(),
                () => new Npc.Archetypes.BeetleArchetype(),
                () => new Npc.Archetypes.SnailArchetype(),
            },
            SmallLife.Waterside => new Func<Npc.ShallowNpcArchetype>[]
            {
                () => new Npc.Archetypes.DragonflyArchetype(),
                () => new Npc.Archetypes.SnailArchetype(),
                () => new Npc.Archetypes.LizardArchetype(),
                () => new Npc.Archetypes.BeetleArchetype(),
            },
            SmallLife.Barren => new Func<Npc.ShallowNpcArchetype>[]
            {
                () => new Npc.Archetypes.LizardArchetype(),
                () => new Npc.Archetypes.BeetleArchetype(),
                () => new Npc.Archetypes.GardenSpiderArchetype(),
            },
            _ => new Func<Npc.ShallowNpcArchetype>[]
            {
                () => new Npc.Archetypes.GardenSpiderArchetype(),
                () => new Npc.Archetypes.CockroachArchetype(),
                () => new Npc.Archetypes.HouseMouseArchetype(),
            },
        };

        int count = rng.Next(min, max + 1);
        for (int i = 0; i < count; i++)
            SpawnShallow(rng, scene, pool[rng.Next(pool.Length)](), areas[rng.Next(areas.Count)]);
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
