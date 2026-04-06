using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

/// <summary>
/// Base class for pyramidal graph factories (Mountain, Peak).
/// Implements the core pyramidal structure generation logic.
/// </summary>
public abstract class PyramidalGraphFactory : NarrationGraphFactory
{
    protected PyramidalGraphFactory(string? sessionPath = null) : base(sessionPath) { }

    /// <summary>
    /// All available feature types with their altitude ranges.
    /// Each feature should have a Bottom and Top variant.
    /// </summary>
    protected abstract List<Type> AllFeatureTypes { get; }

    /// <summary>Build a pyramidal node network with 3-5 levels and return the entry node.</summary>
    protected override NarrationNode BuildNodes(Random rng, int locationId)
    {
        int levelCount = rng.Next(3, 6);
        Console.WriteLine($"PyramidalGraphFactory: Generating {levelCount}-level pyramid for location {locationId}");

        var levelRanges = DivideAltitudeIntoLevels(levelCount);

        var pyramid = new List<List<PyramidalFeatureNode>>();
        for (int level = 0; level < levelCount; level++)
        {
            int spotsInLevel = level + 1;
            var levelSpots   = new List<PyramidalFeatureNode>();
            var (minAlt, maxAlt) = levelRanges[level];

            for (int spotIndex = 0; spotIndex < spotsInLevel; spotIndex++)
            {
                var (bottomNode, topNode) = SampleFeaturePair(rng, minAlt, maxAlt);
                levelSpots.Add(bottomNode);
                Console.WriteLine($"  Level {level + 1} Spot {spotIndex + 1}: {bottomNode.NodeId} + {topNode.NodeId} (altitude {minAlt}-{maxAlt})");
            }

            pyramid.Add(levelSpots);
        }

        ConnectPyramid(pyramid, rng);

        var entryNode = pyramid[levelCount - 1][rng.Next(pyramid[levelCount - 1].Count)];
        Console.WriteLine($"PyramidalGraphFactory: Entry node is {entryNode.NodeId}");
        return entryNode;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private List<(int min, int max)> DivideAltitudeIntoLevels(int levelCount)
    {
        var ranges     = new List<(int, int)>();
        int totalRange = 11;
        float stepSize = totalRange / (float)levelCount;

        for (int i = 0; i < levelCount; i++)
        {
            int min = (int)Math.Floor(i * stepSize);
            int max = (int)Math.Floor((i + 1) * stepSize) - 1;
            if (i == levelCount - 1) max = 10;
            ranges.Add((min, max));
        }

        return ranges;
    }

    private (PyramidalFeatureNode bottom, PyramidalFeatureNode top) SampleFeaturePair(
        Random rng, int minAlt, int maxAlt)
    {
        var candidates = AllFeatureTypes
            .Where(t => typeof(PyramidalFeatureNode).IsAssignableFrom(t))
            .Select(t => (PyramidalFeatureNode)Activator.CreateInstance(t)!)
            .Where(n => n.IsBottomNode && n.MinAltitude <= maxAlt && n.MaxAltitude >= minAlt)
            .ToList();

        if (candidates.Count == 0)
            throw new InvalidOperationException($"No bottom nodes available for altitude range {minAlt}-{maxAlt}");

        var bottomNode = candidates[rng.Next(candidates.Count)];
        var topNode    = (PyramidalFeatureNode)Activator.CreateInstance(bottomNode.PairedNodeType)!;

        ConnectNodes(bottomNode, topNode);
        ConnectNodes(topNode, bottomNode);

        return (bottomNode, topNode);
    }

    private void ConnectPyramid(List<List<PyramidalFeatureNode>> pyramid, Random rng)
    {
        for (int level = 0; level < pyramid.Count; level++)
        {
            var levelSpots = pyramid[level];

            foreach (var bottomNode in levelSpots)
            {
                var topNode = (PyramidalFeatureNode)bottomNode.PossibleOutcomes
                    .OfType<PyramidalFeatureNode>()
                    .First(n => !n.IsBottomNode);

                if (level > 0)
                {
                    var nextLevelBottomNodes = pyramid[level - 1];
                    int connectCount         = Math.Min(rng.Next(1, 3), nextLevelBottomNodes.Count);
                    foreach (var target in nextLevelBottomNodes.OrderBy(_ => rng.Next()).Take(connectCount))
                        ConnectNodes(topNode, target);
                }

                if (levelSpots.Count > 1)
                {
                    var others       = levelSpots.Where(n => n != bottomNode).ToList();
                    int connectCount = Math.Min(rng.Next(1, 3), others.Count);
                    foreach (var target in others.OrderBy(_ => rng.Next()).Take(connectCount))
                    {
                        ConnectNodes(bottomNode, target);
                        ConnectNodes(target, bottomNode);
                    }
                }
            }
        }
    }
}
