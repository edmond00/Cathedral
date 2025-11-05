// GlyphSphereGraph.cs - Graph representation of the glyph sphere for pathfinding
using System;
using System.Collections.Generic;
using System.Numerics;
using Cathedral.Pathfinding;

namespace Cathedral.Glyph
{
    /// <summary>
    /// Graph representation of the glyph sphere where vertices are nodes and edges are defined by triangles
    /// </summary>
    public class GlyphSphereGraph : IPathGraph
    {
        private readonly GlyphSphereCore _core;
        private readonly Dictionary<int, HashSet<int>> _adjacencyList;
        private readonly Dictionary<(int, int), float> _edgeCosts;

        public int NodeCount => _core.VertexCount;

        public GlyphSphereGraph(GlyphSphereCore core)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _adjacencyList = new Dictionary<int, HashSet<int>>();
            _edgeCosts = new Dictionary<(int, int), float>();
            
            BuildGraphFromMesh();
        }

        /// <summary>
        /// Builds the graph connectivity from the sphere mesh triangles
        /// </summary>
        private void BuildGraphFromMesh()
        {
            // Initialize adjacency lists for all vertices
            for (int i = 0; i < NodeCount; i++)
            {
                _adjacencyList[i] = new HashSet<int>();
            }

            // Get triangle indices from the core (we'll need to add this method to GlyphSphereCore)
            var triangles = GetTriangleIndices();
            
            // Build connections based on triangles
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];

                // Connect each pair of vertices in the triangle
                ConnectVertices(v0, v1);
                ConnectVertices(v1, v2);
                ConnectVertices(v2, v0);
            }

            var debugInfo = GetDebugInfo();
            Console.WriteLine($"GlyphSphereGraph built: {debugInfo.nodes} nodes, {debugInfo.edges} edges, {debugInfo.avgConnections:F1} avg connections/node");
            
            // Log some connection examples for verification
            for (int i = 0; i < Math.Min(5, NodeCount); i++)
            {
                var connections = _adjacencyList[i];
                Console.WriteLine($"  Vertex {i}: connected to {connections.Count} vertices: [{string.Join(", ", connections.Take(6))}{(connections.Count > 6 ? "..." : "")}]");
            }
        }

        /// <summary>
        /// Connects two vertices bidirectionally and calculates the edge cost
        /// </summary>
        private void ConnectVertices(int v1, int v2)
        {
            if (v1 == v2) return;

            // Check if connection already exists
            var edge = (Math.Min(v1, v2), Math.Max(v1, v2));
            if (_edgeCosts.ContainsKey(edge)) return;

            // Add to adjacency lists
            _adjacencyList[v1].Add(v2);
            _adjacencyList[v2].Add(v1);

            // Calculate and cache edge cost (Euclidean distance)
            var otkPos1 = _core.GetVertexPosition(v1);
            var otkPos2 = _core.GetVertexPosition(v2);
            var pos1 = new Vector3(otkPos1.X, otkPos1.Y, otkPos1.Z);
            var pos2 = new Vector3(otkPos2.X, otkPos2.Y, otkPos2.Z);
            float distance = Vector3.Distance(pos1, pos2);
            
            // Store cost for both directions (undirected graph)
            _edgeCosts[edge] = distance;
        }

        /// <summary>
        /// Gets triangle indices from the core icosphere mesh
        /// </summary>
        private List<int> GetTriangleIndices()
        {
            // Get the actual triangle indices from the icosphere mesh
            var coreIndices = _core.GetTriangleIndices();
            var triangles = new List<int>();
            
            foreach (uint index in coreIndices)
            {
                triangles.Add((int)index);
            }
            
            return triangles;
        }

        public Vector3 GetNodePosition(int nodeId)
        {
            if (!ContainsNode(nodeId))
                throw new ArgumentOutOfRangeException(nameof(nodeId));
                
            var otkPos = _core.GetVertexPosition(nodeId);
            return new Vector3(otkPos.X, otkPos.Y, otkPos.Z);
        }

        public IEnumerable<int> GetConnectedNodes(int nodeId)
        {
            if (!ContainsNode(nodeId))
                return new List<int>();
                
            return _adjacencyList[nodeId];
        }

        public float GetMoveCost(int fromNode, int toNode)
        {
            if (!ContainsNode(fromNode) || !ContainsNode(toNode))
                return float.MaxValue;

            var edge = (Math.Min(fromNode, toNode), Math.Max(fromNode, toNode));
            return _edgeCosts.TryGetValue(edge, out float cost) ? cost : float.MaxValue;
        }

        public float GetHeuristic(int fromNode, int toNode)
        {
            if (!ContainsNode(fromNode) || !ContainsNode(toNode))
                return float.MaxValue;
                
            var pos1 = GetNodePosition(fromNode);
            var pos2 = GetNodePosition(toNode);
            return Vector3.Distance(pos1, pos2);
        }

        public bool ContainsNode(int nodeId)
        {
            return nodeId >= 0 && nodeId < NodeCount;
        }

        /// <summary>
        /// Finds the closest node to a given position
        /// </summary>
        public int FindClosestNode(Vector3 position)
        {
            int closestNode = -1;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < NodeCount; i++)
            {
                var nodePos = GetNodePosition(i);
                float distance = Vector3.Distance(position, nodePos);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNode = i;
                }
            }

            return closestNode;
        }

        /// <summary>
        /// Gets debug information about the graph
        /// </summary>
        public (int nodes, int edges, float avgConnections) GetDebugInfo()
        {
            int totalConnections = 0;
            foreach (var connections in _adjacencyList.Values)
            {
                totalConnections += connections.Count;
            }
            
            float avgConnections = totalConnections / (float)NodeCount;
            return (NodeCount, _edgeCosts.Count, avgConnections);
        }
    }
}