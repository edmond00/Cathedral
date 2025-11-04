// IPathGraph.cs - Interface for pathfinding graphs
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Cathedral.Pathfinding
{
    /// <summary>
    /// Interface for graphs that support pathfinding operations
    /// </summary>
    public interface IPathGraph
    {
        /// <summary>
        /// Gets the position of a node in 3D space
        /// </summary>
        Vector3 GetNodePosition(int nodeId);

        /// <summary>
        /// Gets all nodes that are directly connected to the given node
        /// </summary>
        IEnumerable<int> GetConnectedNodes(int nodeId);

        /// <summary>
        /// Calculates the cost of moving from one node to another (must be connected)
        /// </summary>
        float GetMoveCost(int fromNode, int toNode);

        /// <summary>
        /// Calculates heuristic distance (typically Euclidean) for A* algorithm
        /// </summary>
        float GetHeuristic(int fromNode, int toNode);

        /// <summary>
        /// Checks if a node exists in the graph
        /// </summary>
        bool ContainsNode(int nodeId);

        /// <summary>
        /// Gets the total number of nodes in the graph
        /// </summary>
        int NodeCount { get; }
    }
}