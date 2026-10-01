using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cathedral.Glyph;
using Cathedral.Glyph.Microworld;
using OpenTK.Mathematics;

namespace Cathedral.Game.History;

/// <summary>
/// The history of a world that is not (yet) the run's: built headless from its seed, off the main
/// thread, so the history viewer can show a moon's past while the player is still choosing it.
///
/// <para>It is the same history the game generates once the moon is confirmed (same terrain through
/// <see cref="HeadlessWorld"/>, same regions, same draws through <see cref="GameRng.ForWorld"/>), which
/// <c>cli/system/world_history_viewer.cli</c> checks by comparing the two hashes. Nothing global is
/// touched, so a preview racing the main thread is harmless.</para>
/// </summary>
public static class WorldHistoryPreview
{
    private static readonly Lazy<(List<Vector3> Positions, List<int>[] Adjacency)> _sphere = new(() =>
    {
        var (positions, triangles) = IcosphereGeometry.Build(Config.GlyphSphere.SphereSubdivisions, Config.GlyphSphere.SphereRadius);
        return (positions, IcosphereGeometry.Adjacency(positions.Count, triangles));
    });

    private static readonly Dictionary<int, Task<WorldHistory>> _cache = new();
    private static readonly object _lock = new();

    /// <summary>The history of the world on <paramref name="worldSeed"/>, built once per process and cached.</summary>
    public static Task<WorldHistory> BuildAsync(int worldSeed)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(worldSeed, out var task) && !task.IsFaulted) return task;
            task = Task.Run(() => Build(worldSeed));
            _cache[worldSeed] = task;
            return task;
        }
    }

    private static WorldHistory Build(int worldSeed)
    {
        var (positions, adjacency) = _sphere.Value;
        var world = HeadlessWorld.Build(worldSeed, positions, adjacency, WorldVariants.Resolve(worldSeed));
        var geography = HistoryGeography.Build(world.Regions, world.VertexCount, v => world.Adjacency[v], v => world.Biome[v]);
        return WorldHistoryGenerator.Generate(geography, worldSeed);
    }
}
