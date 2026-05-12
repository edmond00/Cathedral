// TravelPlanner.cs - Maintains the waypoint queue and computes aggregate travel info
// (duration, vital-heat consumption, encounter chances) by inspecting the biomes
// crossed along the resolved path.
using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Glyph.Microworld;
using Cathedral.Pathfinding;

namespace Cathedral.Game
{
    /// <summary>Result of attempting to toggle a vertex in the waypoint queue.</summary>
    public enum WaypointToggleResult
    {
        /// <summary>Vertex was added as a new waypoint.</summary>
        Added,
        /// <summary>Vertex was already a waypoint and was removed.</summary>
        Removed,
        /// <summary>Queue was full; the oldest waypoint was evicted and the vertex appended.</summary>
        AddedEvictingFirst,
        /// <summary>Vertex sits on a biome forbidden by the active travel constraint.</summary>
        Forbidden,
        /// <summary>Vertex is on a traversable biome but no route reaches it from the
        /// current planning origin (e.g. an isolated island).</summary>
        Unreachable,
        /// <summary>Vertex equals the protagonist's current position; ignored.</summary>
        IgnoredSelf,
    }

    /// <summary>
    /// Aggregate stats for a planned travel path. Probabilities are computed assuming
    /// per-cell independent events; the total chance of at least one encounter is
    /// <c>1 - prod(1 - p_i)</c>.
    /// </summary>
    public sealed class TravelEstimate
    {
        public bool HasPath { get; init; }
        public int CellCount { get; init; }
        public float TotalDurationHours { get; init; }
        public float TotalVitalHeat { get; init; }
        public float TotalEncounterChance { get; init; }
        /// <summary>Sorted list of (creature, chance) entries — chance is "at least once".</summary>
        public IReadOnlyList<(string Creature, float Chance)> EncounterBreakdown { get; init; }
            = Array.Empty<(string, float)>();
        public IReadOnlyList<string> CrossedBiomes { get; init; } = Array.Empty<string>();
        /// <summary>Coarse starvation risk in 0..1, derived from total vital heat. Stub
        /// pending humor-system integration.</summary>
        public float StarvationRisk { get; init; }
    }

    /// <summary>
    /// Owns the queue of travel waypoints and turns them into a continuous list of
    /// vertices using the pathfinding service. Stateless w.r.t. UI — the controller
    /// asks for the latest path/estimate and draws them.
    /// </summary>
    public sealed class TravelPlanner
    {
        private readonly List<int> _waypoints = new();
        private readonly int _maxWaypoints;

        public TravelPlanner(int maxWaypoints = 4)
        {
            if (maxWaypoints < 1) throw new ArgumentOutOfRangeException(nameof(maxWaypoints));
            _maxWaypoints = maxWaypoints;
        }

        public int MaxWaypoints => _maxWaypoints;
        public IReadOnlyList<int> Waypoints => _waypoints;
        public bool HasWaypoints => _waypoints.Count > 0;
        public int Count => _waypoints.Count;
        public int FinalDestination => _waypoints.Count > 0 ? _waypoints[^1] : -1;

        /// <summary>
        /// Toggles the given vertex in the queue. If it's already a waypoint it gets
        /// removed; otherwise it's appended (evicting the oldest if the queue is full).
        /// Cells failing <paramref name="isTraversable"/> are rejected as Forbidden.
        /// Cells passing the biome test but with no resolvable route from the current
        /// planning origin (i.e. <see cref="FinalDestination"/> or the protagonist)
        /// are rejected as Unreachable so the player can keep planning.
        /// </summary>
        public WaypointToggleResult Toggle(int vertex, int protagonistVertex,
            Func<int, bool> isTraversable, Func<int, int, bool> isReachable)
        {
            if (vertex < 0 || vertex == protagonistVertex)
                return WaypointToggleResult.IgnoredSelf;

            int existing = _waypoints.IndexOf(vertex);
            if (existing >= 0)
            {
                _waypoints.RemoveAt(existing);
                return WaypointToggleResult.Removed;
            }

            if (!isTraversable(vertex))
                return WaypointToggleResult.Forbidden;

            int origin = _waypoints.Count > 0 ? _waypoints[^1] : protagonistVertex;
            if (origin >= 0 && origin != vertex && !isReachable(origin, vertex))
                return WaypointToggleResult.Unreachable;

            if (_waypoints.Count >= _maxWaypoints)
            {
                _waypoints.RemoveAt(0);
                _waypoints.Add(vertex);
                return WaypointToggleResult.AddedEvictingFirst;
            }

            _waypoints.Add(vertex);
            return WaypointToggleResult.Added;
        }

        public void Clear() => _waypoints.Clear();

        /// <summary>
        /// Resolves the full vertex sequence the protagonist will walk through.
        /// Returns null if any waypoint segment is unreachable under the constraint.
        /// The returned path includes <paramref name="startVertex"/> as its first node.
        /// </summary>
        public List<int>? ResolvePath(int startVertex, IPathGraph graph, AStar astar)
        {
            if (_waypoints.Count == 0) return null;
            if (startVertex < 0) return null;

            var full = new List<int> { startVertex };
            int current = startVertex;
            foreach (int wp in _waypoints)
            {
                if (wp == current) continue;
                var segment = astar.FindPath(graph, current, wp);
                if (segment == null) return null;
                for (int i = 1; i < segment.Length; i++) full.Add(segment.GetNode(i));
                current = wp;
            }
            return full;
        }

        /// <summary>
        /// Computes the aggregated travel estimate for a resolved path. The first vertex
        /// (the protagonist's current cell) is treated as already-occupied and not counted.
        /// </summary>
        public static TravelEstimate EstimateForPath(IReadOnlyList<int> path,
            Func<int, string?> getBiomeNameForVertex)
        {
            if (path == null || path.Count < 2)
                return new TravelEstimate { HasPath = false };

            float duration = 0f;
            float heat = 0f;
            // For each creature: accumulate log(1-p) so we get product of (1-p) at the end.
            var noEncounterLog = new Dictionary<string, double>(StringComparer.Ordinal);
            var biomes = new List<string>();
            string? lastBiome = null;

            for (int i = 1; i < path.Count; i++)
            {
                string biomeName = getBiomeNameForVertex(path[i]) ?? "unknown";
                if (biomeName != lastBiome)
                {
                    biomes.Add(biomeName);
                    lastBiome = biomeName;
                }
                var info = BiomeTravelDatabase.GetFor(biomeName);
                duration += info.DurationHours;
                heat += info.VitalHeatPerCell;
                foreach (var enc in info.Encounters)
                {
                    double clamped = Math.Clamp(enc.ChancePerCell, 0f, 0.999f);
                    double logTerm = Math.Log(1.0 - clamped);
                    if (!noEncounterLog.ContainsKey(enc.CreatureName))
                        noEncounterLog[enc.CreatureName] = 0.0;
                    noEncounterLog[enc.CreatureName] += logTerm;
                }
            }

            var breakdown = noEncounterLog
                .Select(kv => (kv.Key, Chance: (float)(1.0 - Math.Exp(kv.Value))))
                .OrderByDescending(t => t.Chance)
                .ToList();

            // Combined chance of any encounter at all = 1 - prod over creatures of (1-p).
            double anyNoEncounter = breakdown.Aggregate(1.0, (acc, t) => acc * (1.0 - t.Chance));
            float totalChance = (float)(1.0 - anyNoEncounter);

            // Stub starvation curve until protagonist humor integration: linear ramp once
            // total heat exceeds 20 units, full risk at 60.
            float starvation = MathClamp01((heat - 20f) / 40f);

            return new TravelEstimate
            {
                HasPath = true,
                CellCount = path.Count - 1,
                TotalDurationHours = duration,
                TotalVitalHeat = heat,
                TotalEncounterChance = totalChance,
                EncounterBreakdown = breakdown,
                CrossedBiomes = biomes,
                StarvationRisk = starvation,
            };
        }

        private static float MathClamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
