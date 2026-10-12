using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Cathedral.Glyph.Microworld
{
    /// <summary>
    /// A generated world without a window: the biome of every vertex and the division into regions,
    /// derived exactly as <c>MicroworldInterface.GenerateWorld</c> derives them (noise offset from the
    /// same stream, the same <see cref="WorldClassifier"/>, the same region input). For audits that need the world a
    /// player would stand in, not one like it.
    ///
    /// <para>Touches no global state: the terrain stream is drawn through <see cref="GameRng.ForWorld"/>
    /// with the world's own seed, which yields exactly what <c>GameRng.For("world-terrain")</c> yields
    /// once that seed is the run's. Safe off the main thread, which the history viewer relies on.</para>
    /// </summary>
    public sealed class HeadlessWorld
    {
        public required int Seed { get; init; }
        public required WorldVariant Variant { get; init; }
        public required string[] Biome { get; init; }
        public required List<int>[] Adjacency { get; init; }
        public required WorldRegionMap Regions { get; init; }

        public int VertexCount => Biome.Length;

        /// <param name="variant">The variant to build to; the seed's own when null. The viewer passes
        /// <c>WorldVariants.Resolve</c>, so a <c>--world-variant</c> run previews the world it will get.</param>
        public static HeadlessWorld Build(int seed, List<Vector3> positions, List<int>[] adjacency,
                                          WorldVariant? variant = null)
        {
            variant ??= WorldVariants.ForSeed(seed);
            var worldRng = GameRng.ForWorld(seed, "world-terrain");
            var offset = new Vector3(
                (float)(worldRng.NextDouble() * 20000.0 - 10000.0),
                (float)(worldRng.NextDouble() * 20000.0 - 10000.0),
                (float)(worldRng.NextDouble() * 20000.0 - 10000.0));

            int n = positions.Count;
            var classified = WorldClassifier.Classify(variant.Shape, n, v => positions[v], v => adjacency[v], offset);
            var biome = classified.Biome;
            var settlement = classified.Settlement;

            var regions = WorldRegionMap.Build(new WorldRegionInput
            {
                VertexCount     = n,
                Neighbours      = v => adjacency[v],
                IsLand          = v => !BiomeDatabase.WaterBiomes.Contains(biome[v]),
                IsSettleable    = v => !BiomeDatabase.WaterBiomes.Contains(biome[v])
                                       && !BiomeDatabase.MountainBiomes.Contains(biome[v]),
                SettlementNoise = v => settlement[v],
                StepCostDays    = v => BiomeTravelDatabase.GetFor(biome[v]).DurationDays,
                Position        = v => positions[v],
            });

            return new HeadlessWorld { Seed = seed, Variant = variant, Biome = biome, Adjacency = adjacency, Regions = regions };
        }
    }
}
