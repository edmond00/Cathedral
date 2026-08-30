// BiomeTravelConstraint.cs - Travel constraints driven by per-vertex biome lookups.
using System;
using System.Collections.Generic;
using Cathedral.Pathfinding;

namespace Cathedral.Glyph.Microworld
{
    /// <summary>
    /// Travel constraint that allows / forbids nodes based on the biome at each vertex.
    /// The set of forbidden biomes is configurable so the same class can model
    /// "on foot" (forbids sea/ocean) or future modes ("by ship": forbid every non-water
    /// biome, allow ports as entry/exit points).
    /// </summary>
    public sealed class BiomeTravelConstraint : ITravelConstraint
    {
        private readonly Func<int, string?> _getBiomeNameForVertex;
        private readonly HashSet<string> _forbiddenBiomes;

        public BiomeTravelConstraint(
            string name,
            Func<int, string?> getBiomeNameForVertex,
            IEnumerable<string> forbiddenBiomes)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _getBiomeNameForVertex = getBiomeNameForVertex
                ?? throw new ArgumentNullException(nameof(getBiomeNameForVertex));
            _forbiddenBiomes = new HashSet<string>(forbiddenBiomes ?? Array.Empty<string>(), StringComparer.Ordinal);
        }

        public string Name { get; }

        public IReadOnlyCollection<string> ForbiddenBiomes => _forbiddenBiomes;

        public bool IsTraversable(int nodeId)
        {
            string? biome = _getBiomeNameForVertex(nodeId);
            if (biome == null) return true; // unknown vertex — let the base graph decide
            return !_forbiddenBiomes.Contains(biome);
        }

        // No per-edge cost adjustment for now — biome travel cost is computed by the
        // travel planner from BiomeTravelDatabase, not folded into the A* metric.
        public float GetCostMultiplier(int fromNode, int toNode) => 1.0f;
    }

    /// <summary>
    /// Factory helpers for the travel constraints currently supported.
    /// New travel modes (ship, mount, …) should be added here.
    /// </summary>
    public static class TravelConstraints
    {
        /// <summary>
        /// Constraint used by the protagonist's default on-foot travel: forbids sea and ocean.
        /// </summary>
        public static BiomeTravelConstraint Land(Func<int, string?> getBiomeNameForVertex)
            => new BiomeTravelConstraint("land", getBiomeNameForVertex, BiomeTravelDatabase.LandForbiddenBiomes);
    }
}
