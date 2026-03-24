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
    protected string? _sessionPath;
        public PyramidalGraphFactory(string? sessionPath = null)
    {
        _sessionPath = sessionPath;
    }
        /// <summary>
    /// All available feature types with their altitude ranges.
    /// Each feature should have a Bottom and Top variant.
    /// </summary>
    protected abstract List<Type> AllFeatureTypes { get; }
    
    /// <summary>
    /// Generate a pyramidal graph with 3-5 levels.
    /// </summary>
    public override NarrationNode GenerateGraph(int locationId)
    {
        var rng = CreateSeededRandom(locationId);
        
        // Sample number of levels (3-5)
        int levelCount = rng.Next(3, 6);
        
        Console.WriteLine($"PyramidalGraphFactory: Generating {levelCount}-level pyramid for location {locationId}");
        
        // Divide altitude 0-10 into level ranges
        var levelRanges = DivideAltitudeIntoLevels(levelCount);
        
        // Create pyramid structure: level 1 = 1 spot, level 2 = 2 spots, etc.
        var pyramid = new List<List<PyramidalFeatureNode>>();
        
        for (int level = 0; level < levelCount; level++)
        {
            int spotsInLevel = level + 1;
            var levelSpots = new List<PyramidalFeatureNode>();
            
            var (minAlt, maxAlt) = levelRanges[level];
            
            for (int spotIndex = 0; spotIndex < spotsInLevel; spotIndex++)
            {
                // Sample a feature that fits this altitude range
                var (bottomNode, topNode) = SampleFeaturePair(rng, minAlt, maxAlt);
                
                levelSpots.Add(bottomNode);
                
                Console.WriteLine($"  Level {level + 1} Spot {spotIndex + 1}: {bottomNode.NodeId} + {topNode.NodeId} (altitude {minAlt}-{maxAlt})");
            }
            
            pyramid.Add(levelSpots);
        }
        
        // Connect nodes
        ConnectPyramid(pyramid, rng);
        
        // Entry node is a bottom node from the highest level (last level)
        var entryNode = pyramid[levelCount - 1][rng.Next(pyramid[levelCount - 1].Count)];
        
        Console.WriteLine($"PyramidalGraphFactory: Entry node is {entryNode.NodeId}");
        
        // Log the graph structure
        WriteGraphToLog(entryNode, locationId, _sessionPath);
        
        return entryNode;
    }
    
    /// <summary>
    /// Divide altitude 0-10 into level ranges.
    /// Returns list of (minAltitude, maxAltitude) tuples.
    /// </summary>
    private List<(int min, int max)> DivideAltitudeIntoLevels(int levelCount)
    {
        var ranges = new List<(int, int)>();
        
        // Total altitude range is 0-10 (11 values)
        int totalRange = 11;
        float stepSize = totalRange / (float)levelCount;
        
        for (int i = 0; i < levelCount; i++)
        {
            int min = (int)Math.Floor(i * stepSize);
            int max = (int)Math.Floor((i + 1) * stepSize) - 1;
            
            // Last level gets the remainder
            if (i == levelCount - 1)
                max = 10;
            
            ranges.Add((min, max));
        }
        
        return ranges;
    }
    
    /// <summary>
    /// Sample a bottom/top feature pair that fits the altitude range.
    /// </summary>
    private (PyramidalFeatureNode bottom, PyramidalFeatureNode top) SampleFeaturePair(Random rng, int minAlt, int maxAlt)
    {
        // Get all bottom nodes that can exist in this altitude range
        var candidateBottomNodes = AllFeatureTypes
            .Where(t => typeof(PyramidalFeatureNode).IsAssignableFrom(t))
            .Select(t => (PyramidalFeatureNode)Activator.CreateInstance(t)!)
            .Where(n => n.IsBottomNode && n.MinAltitude <= maxAlt && n.MaxAltitude >= minAlt)
            .ToList();
        
        if (candidateBottomNodes.Count == 0)
            throw new InvalidOperationException($"No bottom nodes available for altitude range {minAlt}-{maxAlt}");
        
        // Sample one bottom node
        var bottomNode = candidateBottomNodes[rng.Next(candidateBottomNodes.Count)];
        
        // Create its paired top node
        var topNode = (PyramidalFeatureNode)Activator.CreateInstance(bottomNode.PairedNodeType)!;
        
        // Connect bottom <-> top bidirectionally
        ConnectNodes(bottomNode, topNode);
        ConnectNodes(topNode, bottomNode);
        
        return (bottomNode, topNode);
    }
    
    /// <summary>
    /// Connect pyramid nodes following the rules:
    /// - Top nodes connect to bottom nodes of the next level up
    /// - Bottom nodes connect to 1-2 other bottom nodes at the same level
    /// </summary>
    private void ConnectPyramid(List<List<PyramidalFeatureNode>> pyramid, Random rng)
    {
        for (int level = 0; level < pyramid.Count; level++)
        {
            var levelSpots = pyramid[level];
            
            foreach (var bottomNode in levelSpots)
            {
                // Get the paired top node (already connected bidirectionally)
                var topNode = (PyramidalFeatureNode)bottomNode.PossibleOutcomes
                    .OfType<PyramidalFeatureNode>()
                    .First(n => !n.IsBottomNode);
                
                // Connect top node to bottom nodes of next level (if not at summit)
                if (level > 0)
                {
                    var nextLevelBottomNodes = pyramid[level - 1];
                    
                    // Connect to 1-2 random bottom nodes from the level above
                    int connectCount = Math.Min(rng.Next(1, 3), nextLevelBottomNodes.Count);
                    var targets = nextLevelBottomNodes.OrderBy(_ => rng.Next()).Take(connectCount).ToList();
                    
                    foreach (var target in targets)
                    {
                        ConnectNodes(topNode, target);
                    }
                }
                
                // Connect bottom nodes within the same level
                if (levelSpots.Count > 1)
                {
                    var otherBottomNodes = levelSpots.Where(n => n != bottomNode).ToList();
                    
                    // Connect to 1-2 other bottom nodes at the same level (bidirectional)
                    int connectCount = Math.Min(rng.Next(1, 3), otherBottomNodes.Count);
                    var targets = otherBottomNodes.OrderBy(_ => rng.Next()).Take(connectCount).ToList();
                    
                    foreach (var target in targets)
                    {
                        // Bidirectional connection
                        ConnectNodes(bottomNode, target);
                        ConnectNodes(target, bottomNode);
                    }
                }
            }
        }
    }
}
