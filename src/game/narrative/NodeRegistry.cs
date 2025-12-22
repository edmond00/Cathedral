using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative.Nodes;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Registry of all available narration nodes.
/// Replaces the old ForestNarrationNodeGenerator.
/// </summary>
public static class NodeRegistry
{
    private static readonly List<NarrationNode> _allNodes = new()
    {
        new ClearingNode(),
        new StreamNode()
    };
    
    /// <summary>
    /// Gets a node by its ID.
    /// </summary>
    public static NarrationNode? GetNode(string nodeId)
    {
        return _allNodes.FirstOrDefault(n => n.NodeId == nodeId);
    }
    
    /// <summary>
    /// Gets a random entry node.
    /// </summary>
    public static NarrationNode GetRandomEntryNode()
    {
        var entryNodes = _allNodes.Where(n => n.IsEntryNode).ToList();
        if (entryNodes.Count == 0)
            return _allNodes[0]; // Fallback
        
        var rng = new Random();
        return entryNodes[rng.Next(entryNodes.Count)];
    }
    
    /// <summary>
    /// Gets all nodes.
    /// </summary>
    public static List<NarrationNode> GetAllNodes()
    {
        return new List<NarrationNode>(_allNodes);
    }
}
