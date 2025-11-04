// Path.cs - Represents a path through the graph
using System.Collections.Generic;
using System.Numerics;

namespace Cathedral.Pathfinding
{
    /// <summary>
    /// Represents a path through the graph with nodes and positions
    /// </summary>
    public class Path
    {
        public List<int> Nodes { get; }
        public List<Vector3> Positions { get; }
        public float TotalCost { get; }

        public Path(List<int> nodes, List<Vector3> positions, float totalCost)
        {
            Nodes = nodes ?? new List<int>();
            Positions = positions ?? new List<Vector3>();
            TotalCost = totalCost;
        }

        /// <summary>
        /// Gets the number of waypoints in the path
        /// </summary>
        public int Length => Nodes.Count;

        /// <summary>
        /// Checks if the path is empty
        /// </summary>
        public bool IsEmpty => Nodes.Count == 0;

        /// <summary>
        /// Gets the start node of the path
        /// </summary>
        public int StartNode => Nodes.Count > 0 ? Nodes[0] : -1;

        /// <summary>
        /// Gets the end node of the path
        /// </summary>
        public int EndNode => Nodes.Count > 0 ? Nodes[Nodes.Count - 1] : -1;

        /// <summary>
        /// Gets the position at a specific index in the path
        /// </summary>
        public Vector3 GetPosition(int index)
        {
            if (index >= 0 && index < Positions.Count)
                return Positions[index];
            return Vector3.Zero;
        }

        /// <summary>
        /// Gets the node ID at a specific index in the path
        /// </summary>
        public int GetNode(int index)
        {
            if (index >= 0 && index < Nodes.Count)
                return Nodes[index];
            return -1;
        }
    }
}