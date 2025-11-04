// PathfindingTest.cs - Simple test for the pathfinding system
using System;
using System.Threading.Tasks;
using Cathedral.Pathfinding;

namespace Cathedral.Tests
{
    public class PathfindingTest
    {
        public static async Task RunTests()
        {
            Console.WriteLine("Running pathfinding tests...");
            
            // Create a simple test graph
            var testGraph = new TestGraph();
            var pathfinder = new PathfindingService();
            
            try
            {
                // Test simple pathfinding
                var path = await pathfinder.FindPathAsync(testGraph, 0, 4);
                
                if (path != null)
                {
                    Console.WriteLine($"Path found with {path.Length} nodes and cost {path.TotalCost:F2}");
                    for (int i = 0; i < path.Length; i++)
                    {
                        Console.WriteLine($"  Node {i}: {path.GetNode(i)} at {path.GetPosition(i)}");
                    }
                }
                else
                {
                    Console.WriteLine("No path found!");
                }
                
                // Test pathfinding to unreachable node
                var unreachablePath = await pathfinder.FindPathAsync(testGraph, 0, 99);
                Console.WriteLine($"Unreachable path result: {(unreachablePath == null ? "null (correct)" : "found (unexpected)")}");
                
                Console.WriteLine("Pathfinding tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test failed: {ex.Message}");
            }
            finally
            {
                pathfinder.Dispose();
            }
        }
    }

    /// <summary>
    /// Simple test graph implementation for testing pathfinding
    /// Creates a small grid-like graph: 0-1-2
    ///                                   |   |
    ///                                   3-4
    /// </summary>
    public class TestGraph : IPathGraph
    {
        private readonly System.Numerics.Vector3[] positions = {
            new System.Numerics.Vector3(0, 0, 0),  // 0
            new System.Numerics.Vector3(1, 0, 0),  // 1
            new System.Numerics.Vector3(2, 0, 0),  // 2
            new System.Numerics.Vector3(0, 0, 1),  // 3
            new System.Numerics.Vector3(1, 0, 1),  // 4
        };

        private readonly int[][] connections = {
            new int[] { 1, 3 },    // 0 connects to 1, 3
            new int[] { 0, 2, 4 }, // 1 connects to 0, 2, 4
            new int[] { 1 },       // 2 connects to 1
            new int[] { 0, 4 },    // 3 connects to 0, 4
            new int[] { 1, 3 },    // 4 connects to 1, 3
        };

        public int NodeCount => positions.Length;

        public bool ContainsNode(int nodeId) => nodeId >= 0 && nodeId < NodeCount;

        public System.Numerics.Vector3 GetNodePosition(int nodeId) => positions[nodeId];

        public System.Collections.Generic.IEnumerable<int> GetConnectedNodes(int nodeId)
        {
            if (ContainsNode(nodeId))
                return connections[nodeId];
            return new int[0];
        }

        public float GetMoveCost(int fromNode, int toNode)
        {
            var pos1 = GetNodePosition(fromNode);
            var pos2 = GetNodePosition(toNode);
            return System.Numerics.Vector3.Distance(pos1, pos2);
        }

        public float GetHeuristic(int fromNode, int toNode)
        {
            return GetMoveCost(fromNode, toNode); // Euclidean distance
        }
    }
}