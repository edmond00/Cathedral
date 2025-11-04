// AStar.cs - A* pathfinding algorithm implementation
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Pathfinding
{
    /// <summary>
    /// Exception thrown when pathfinding fails
    /// </summary>
    public class PathfindingException : Exception
    {
        public PathfindingException(string message) : base(message) { }
        public PathfindingException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Node used in A* pathfinding algorithm
    /// </summary>
    internal class AStarNode
    {
        public int NodeId { get; }
        public float GCost { get; set; }  // Cost from start
        public float HCost { get; set; }  // Heuristic cost to end
        public float FCost => GCost + HCost;  // Total cost
        public AStarNode? Parent { get; set; }

        public AStarNode(int nodeId)
        {
            NodeId = nodeId;
            GCost = float.MaxValue;
            HCost = 0;
            Parent = null;
        }
    }

    /// <summary>
    /// A* pathfinding algorithm implementation
    /// </summary>
    public class AStar
    {
        /// <summary>
        /// Finds the shortest path between two nodes using A* algorithm
        /// </summary>
        /// <param name="graph">The graph to search</param>
        /// <param name="startNode">Starting node ID</param>
        /// <param name="endNode">Target node ID</param>
        /// <returns>Path from start to end, or null if no path exists</returns>
        /// <exception cref="PathfindingException">Thrown when pathfinding fails due to invalid parameters</exception>
        public Path? FindPath(IPathGraph graph, int startNode, int endNode)
        {
            if (graph == null)
                throw new PathfindingException("Graph cannot be null");

            if (!graph.ContainsNode(startNode))
                throw new PathfindingException($"Start node {startNode} does not exist in graph");

            if (!graph.ContainsNode(endNode))
                throw new PathfindingException($"End node {endNode} does not exist in graph");

            if (startNode == endNode)
            {
                // Same start and end node
                var sameNodePositions = new List<System.Numerics.Vector3> { graph.GetNodePosition(startNode) };
                return new Path(new List<int> { startNode }, sameNodePositions, 0);
            }

            var openSet = new SortedSet<AStarNode>(new AStarNodeComparer());
            var allNodes = new Dictionary<int, AStarNode>();
            var closedSet = new HashSet<int>();

            // Initialize start node
            var startAStarNode = new AStarNode(startNode)
            {
                GCost = 0,
                HCost = graph.GetHeuristic(startNode, endNode)
            };

            openSet.Add(startAStarNode);
            allNodes[startNode] = startAStarNode;

            while (openSet.Count > 0)
            {
                var current = openSet.Min!;
                openSet.Remove(current);
                closedSet.Add(current.NodeId);

                // Check if we reached the target
                if (current.NodeId == endNode)
                {
                    return ReconstructPath(graph, current);
                }

                // Check all neighbors
                foreach (int neighborId in graph.GetConnectedNodes(current.NodeId))
                {
                    if (closedSet.Contains(neighborId))
                        continue;

                    float moveCost = graph.GetMoveCost(current.NodeId, neighborId);
                    float tentativeGCost = current.GCost + moveCost;

                    if (!allNodes.ContainsKey(neighborId))
                    {
                        allNodes[neighborId] = new AStarNode(neighborId);
                    }

                    var neighbor = allNodes[neighborId];

                    if (tentativeGCost < neighbor.GCost)
                    {
                        // Remove from open set if it was there
                        if (openSet.Contains(neighbor))
                        {
                            openSet.Remove(neighbor);
                        }

                        // Update neighbor
                        neighbor.Parent = current;
                        neighbor.GCost = tentativeGCost;
                        neighbor.HCost = graph.GetHeuristic(neighborId, endNode);

                        // Add to open set
                        openSet.Add(neighbor);
                    }
                }
            }

            // No path found
            return null;
        }

        /// <summary>
        /// Reconstructs the path from the end node back to the start
        /// </summary>
        private Path ReconstructPath(IPathGraph graph, AStarNode endNode)
        {
            var nodes = new List<int>();
            var positions = new List<System.Numerics.Vector3>();
            float totalCost = endNode.GCost;

            var current = endNode;
            while (current != null)
            {
                nodes.Insert(0, current.NodeId);
                positions.Insert(0, graph.GetNodePosition(current.NodeId));
                current = current.Parent;
            }

            return new Path(nodes, positions, totalCost);
        }
    }

    /// <summary>
    /// Comparer for A* nodes to maintain proper ordering in SortedSet
    /// </summary>
    internal class AStarNodeComparer : IComparer<AStarNode>
    {
        public int Compare(AStarNode? x, AStarNode? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            // First compare by F cost
            int fComparison = x.FCost.CompareTo(y.FCost);
            if (fComparison != 0) return fComparison;

            // If F costs are equal, compare by H cost (prefer lower H cost)
            int hComparison = x.HCost.CompareTo(y.HCost);
            if (hComparison != 0) return hComparison;

            // If both F and H are equal, use node ID for consistent ordering
            return x.NodeId.CompareTo(y.NodeId);
        }
    }
}