using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Shared;

/// <summary>
/// The common half of the eight hot and cold wilderness factories (desert, hot steppe, jungle,
/// canyon, sea ice, glacier, snowfield, cold steppe — see <c>ClimateRule</c>).
///
/// <para>The temperate wilderness factories each carry a private copy of the same few helpers —
/// spawn a roaming beast, spawn a pinned creature, chain the areas with paths — and differ only in
/// their names. Eight more copies would have been eight more places for the affinity resolver or the
/// roaming rule to be forgotten in, so the new ones share these. What each factory decides for itself
/// is everything that makes the place what it is: its identity roll, its areas and their contents,
/// its climb, its beasts.</para>
///
/// <para>Uninhabited by design, like plain and peak: hot and cold country has no settlement yet, so
/// nothing here spawns a person.</para>
/// </summary>
public abstract class ClimateSceneFactory : SceneFactory
{
    protected ClimateSceneFactory(string? sessionPath) : base(sessionPath) { }

    /// <summary>Every walkable area, in the order the sections were built — the beasts' range.</summary>
    protected readonly List<Area> _allAreas = new();

    /// <summary>Builds a section around <paramref name="areas"/>, registers it, and adds them to the range.</summary>
    protected Section AddSection(Scene scene, string name, string description,
                                 Func<int, IFightAreaGenerator> generator, IEnumerable<Area> areas)
    {
        var section = new Section(name, new() { description }, generator);
        section.Areas.AddRange(areas);
        scene.Sections.Add(section);
        RegisterAll(scene, section);
        _allAreas.AddRange(section.Areas);
        return section;
    }

    /// <summary>
    /// Joins consecutive areas of <paramref name="chain"/> with a path, the way every wilderness
    /// factory does. <paramref name="name"/> picks the path's name from the two ends.
    /// </summary>
    protected static void ConnectChain(Scene scene, IReadOnlyList<Area> chain, Func<Area, Area, string> name,
                                       string surface, string[] moods)
    {
        for (int i = 0; i < chain.Count - 1; i++)
        {
            var a = chain[i];
            var b = chain[i + 1];
            scene.ConnectAreasBidirectional(a, b);
            var path = new PathPointOfInterest(
                a, b, PathPointOfInterest.NameFor(a, b, name(a, b)),
                new() { $"A way {surface} between {a.DisplayName.ToLowerInvariant()} and {b.DisplayName.ToLowerInvariant()}" },
                moods);
            a.PointsOfInterest.Add(path);
            b.PointsOfInterest.Add(path);
            path.Register(scene);
        }
    }

    /// <summary>
    /// A climb from <paramref name="bottom"/> to <paramref name="top"/> — the only way up, so no path
    /// joins them — and from the top a road to every other area of the place. The top must already be
    /// in a section and carry something, or the climb arrives nowhere.
    /// </summary>
    protected static void AddViewpoint(Scene scene, Area bottom, Area top, string climbName,
                                       string climbDescription, string[] moods, bool icy = false)
    {
        new CliffPointOfInterest(bottom, top, climbName, new() { climbDescription }, icyCliff: icy, moods: moods)
            .AttachTo(scene);
        AddLandscapes(scene, top, scene.AllAreas);
    }

    /// <summary>
    /// The sit spots, hiding places, shortcuts and diggable ground every location gets.
    /// <paramref name="climbedTo"/> are the areas only a climb reaches: they may be furnished, but no
    /// shortcut may end on one, or the climb would have a way round it.
    /// </summary>
    protected static void Furnish(Random rng, Scene scene, FurnitureSubfactory.Setting setting, params Area?[] climbedTo)
    {
        var outdoors = scene.OutdoorAreas;
        var ground = outdoors.Where(a => !climbedTo.Any(c => c != null && c.Id == a.Id)).ToList();
        FurnitureSubfactory.AddSitSpots(rng, outdoors, setting);
        FurnitureSubfactory.AddHidingPlaces(rng, outdoors, setting);
        FurnitureSubfactory.AddShortcuts(rng, scene, ground, setting);
        FurnitureSubfactory.AddExtractionPoints(rng, ground, setting);
    }

    /// <summary>
    /// Rolls for a named beast and, if it lands, gives it a roaming day over <paramref name="range"/>
    /// (every area by default). Affinity persists per NPC through the location state's resolver, so
    /// a beast appeased here is still appeased on the next arrival.
    /// </summary>
    protected void TrySpawnBeast(Random rng, Scene scene, NamedNpcArchetype archetype, double chance,
                                 IReadOnlyList<Area>? range = null)
    {
        if (rng.NextDouble() > chance) return;
        range ??= _allAreas;
        if (range.Count == 0) return;

        var area = range[rng.Next(range.Count)];
        var entity = archetype.Spawn(rng, area.ContextDescription,
            _locationState != null ? _locationState.AffinityFor : null);
        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = RoamingSchedule(rng, range);
    }

    /// <summary>Rolls for a shallow creature pinned to one area of <paramref name="range"/> (every area by default).</summary>
    protected void TrySpawnCreature(Random rng, Scene scene, ShallowNpcArchetype archetype, double chance,
                                    IReadOnlyList<Area>? range = null)
        => TrySpawnShallow(rng, scene, archetype, range ?? _allAreas, chance);
}
