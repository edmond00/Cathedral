using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Abstract base class for procedural narration graph generation.
/// Each location gets a unique graph instance seeded by locationId for determinism.
/// </summary>
public abstract class NarrationGraphFactory
{
    /// <summary>
    /// Generates a unique narration graph for the given location.
    /// Returns the entry node with PossibleOutcomes populated.
    /// </summary>
    /// <param name="locationId">Location identifier (e.g., vertex index) used as RNG seed</param>
    /// <returns>Entry node of the generated graph</returns>
    public abstract NarrationNode GenerateGraph(int locationId);
    
    /// <summary>
    /// Creates a seeded random number generator for deterministic graph generation.
    /// Same locationId always produces the same graph structure.
    /// </summary>
    protected Random CreateSeededRandom(int locationId)
    {
        return new Random(locationId);
    }
    
    /// <summary>
    /// Creates a new instance of a narration node.
    /// </summary>
    protected T CreateNode<T>() where T : NarrationNode, new()
    {
        return new T();
    }
    
    /// <summary>
    /// Connects two nodes by adding 'to' to 'from.PossibleOutcomes'.
    /// Prevents self-loops (a node connecting to itself).
    /// </summary>
    protected void ConnectNodes(NarrationNode from, NarrationNode to)
    {
        // Prevent self-loops
        if (from == to)
            return;
            
        if (!from.PossibleOutcomes.Contains(to))
        {
            from.PossibleOutcomes.Add(to);
        }
    }
    
    /// <summary>
    /// Writes the generated graph structure to a log file in the current LLM session folder.
    /// Traverses the graph breadth-first to show all nodes and their connections.
    /// Requires a LlamaServerManager to be initialized with a session directory.
    /// </summary>
    /// <param name="entryNode">The entry node of the graph</param>
    /// <param name="locationId">Location identifier for the filename</param>
    /// <param name="sessionPath">Session log directory path (optional)</param>
    protected void WriteGraphToLog(NarrationNode entryNode, int locationId, string? sessionPath = null)
    {
        try
        {
            // If no session path provided, create in logs root
            if (string.IsNullOrEmpty(sessionPath))
            {
                var logsDir = Path.Combine(Environment.CurrentDirectory, "logs");
                if (!Directory.Exists(logsDir))
                {
                    Directory.CreateDirectory(logsDir);
                }
                sessionPath = logsDir;
                Console.WriteLine($"NarrationGraphFactory: No active LLM session, writing graph to logs root");
            }
            
            var logPath = Path.Combine(sessionPath, $"graph_location_{locationId}.txt");
            
            using (var writer = new StreamWriter(logPath))
            {
                writer.WriteLine($"=== Narration Graph for Location {locationId} ===");
                writer.WriteLine($"Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"Entry Node: {entryNode.NodeId}");
                writer.WriteLine();
                
                // Breadth-first traversal to avoid infinite loops from circular references
                var visited = new HashSet<string>();
                var queue = new Queue<NarrationNode>();
                queue.Enqueue(entryNode);
                visited.Add(entryNode.NodeId);
                
                int nodeCount = 0;
                while (queue.Count > 0)
                {
                    var node = queue.Dequeue();
                    nodeCount++;
                    
                    writer.WriteLine($"Node: {node.NodeId}");
                    writer.WriteLine($"  Display Name: {node.DisplayName}");
                    writer.WriteLine($"  Is Entry Node: {node.IsEntryNode}");
                    writer.WriteLine($"  Context: {node.ContextDescription}");
                    writer.WriteLine($"  Transition: {node.TransitionDescription}");
                    writer.WriteLine($"  Keywords: {string.Join(", ", node.NodeKeywordsInContext.Select(k => k.Keyword))}");
                    
                    // List items discovered via reflection
                    var items = node.GetAvailableItems();
                    if (items.Count > 0)
                    {
                        writer.WriteLine($"  Items ({items.Count}):");
                        foreach (var item in items)
                        {
                            writer.WriteLine($"    - {item.DisplayName} ({item.ItemId}): {string.Join(", ", item.OutcomeKeywordsInContext.Select(k => k.Keyword))}");
                        }
                    }
                    
                    // List possible encounters
                    if (node.PossibleEncounters.Count > 0)
                    {
                        writer.WriteLine($"  Encounters ({node.PossibleEncounters.Count}):");
                        foreach (var slot in node.PossibleEncounters)
                        {
                            writer.WriteLine($"    ! {slot.Archetype.ArchetypeId} ({slot.SpawnChance:P0} spawn chance, max {slot.MaxCount})");
                        }
                    }
                    
                    // List connected nodes
                    var childNodes = node.PossibleOutcomes.OfType<NarrationNode>().ToList();
                    if (childNodes.Count > 0)
                    {
                        writer.WriteLine($"  Connected Nodes ({childNodes.Count}):");
                        foreach (var childNode in childNodes)
                        {
                            writer.WriteLine($"    -> {childNode.NodeId}");
                            
                            // Queue unvisited nodes for traversal
                            if (!visited.Contains(childNode.NodeId))
                            {
                                visited.Add(childNode.NodeId);
                                queue.Enqueue(childNode);
                            }
                        }
                    }
                    else
                    {
                        writer.WriteLine($"  Connected Nodes: (none)");
                    }
                    
                    writer.WriteLine();
                }
                
                writer.WriteLine($"=== Graph Summary ===");
                writer.WriteLine($"Total Nodes: {nodeCount}");
                writer.WriteLine($"Unique Node Types: {visited.Count}");
            }
            
            Console.WriteLine($"NarrationGraphFactory: Graph structure written to {logPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NarrationGraphFactory: Failed to write graph log: {ex.Message}");
        }
    }
}
