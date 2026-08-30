using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Abstract base for procedural narration graph generation.
/// Each location gets a unique <see cref="NarrationGraph"/> seeded by locationId.
///
/// Subclass responsibility:
///   <see cref="BuildNodes"/>  — build the NarrationNode network and return the entry node.
///
/// NPC population is not the graph factory's concern: the live scene system places its NPCs into
/// nodes per time period through <c>SceneNpcPlacement</c>.
/// </summary>
public abstract class NarrationGraphFactory
{
    protected readonly string? _sessionPath;

    protected NarrationGraphFactory(string? sessionPath = null)
    {
        _sessionPath = sessionPath;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds and returns a complete <see cref="NarrationGraph"/> for the given location.
    /// Deterministic: same locationId always produces the same graph structure.
    /// </summary>
    public NarrationGraph GenerateGraph(int locationId)
    {
        var rng      = CreateSeededRandom(locationId);
        var entry    = BuildNodes(rng, locationId);
        var allNodes = CollectAllNodes(entry);
        var graph    = new NarrationGraph(entry, allNodes);
        WriteGraphToLog(graph, locationId);
        return graph;
    }

    // ── Abstract / virtual hooks ──────────────────────────────────────────────

    /// <summary>
    /// Build the NarrationNode network (area nodes, transversals, observations, etc.)
    /// and return the entry node.  PossibleOutcomes must be populated before returning.
    /// </summary>
    protected abstract NarrationNode BuildNodes(Random rng, int locationId);

    // ── Shared helpers ────────────────────────────────────────────────────────

    protected Random CreateSeededRandom(int locationId)
        => new Random(locationId);


    // ── Private helpers ───────────────────────────────────────────────────────

    protected virtual IReadOnlyDictionary<string, NarrationNode> CollectAllNodes(NarrationNode entry)
    {
        var dict  = new Dictionary<string, NarrationNode>();
        var queue = new Queue<NarrationNode>();
        queue.Enqueue(entry);
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (dict.ContainsKey(node.NodeId)) continue;
            dict[node.NodeId] = node;
            foreach (var child in node.PossibleOutcomes.OfType<NarrationNode>())
                queue.Enqueue(child);
        }
        return dict;
    }

    // ── Graph logging ─────────────────────────────────────────────────────────

    protected void WriteGraphToLog(NarrationGraph graph, int locationId)
    {
        // A development instrument: one file per location visited, describing the narration graph.
        // Shipped builds write only log.txt. See Config.Debug.VerboseFileLogging.
        if (!Cathedral.Config.Debug.VerboseFileLogging) return;

        try
        {
            string logDir = string.IsNullOrEmpty(_sessionPath)
                ? Path.Combine(Environment.CurrentDirectory, "logs")
                : _sessionPath;

            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, $"graph_location_{locationId}.txt");

            using var writer = new StreamWriter(logPath);
            writer.WriteLine($"=== Narration Graph for Location {locationId} ===");
            writer.WriteLine($"Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Entry Node: {graph.EntryNode.NodeId}");
            writer.WriteLine();

            // Nodes
            int nodeCount = 0;
            foreach (var node in graph.AllNodes.Values)
            {
                nodeCount++;
                writer.WriteLine($"Node: {node.NodeId}");
                writer.WriteLine($"  Context:    {node.ContextDescription}");
                writer.WriteLine($"  Transition: {node.TransitionDescription}");
                writer.WriteLine($"  Outcomes:   {node.GetAllDirectConcreteOutcomes().Count}");


                var observations = node.PossibleOutcomes.OfType<ObservationObject>().ToList();
                if (observations.Count > 0)
                {
                    writer.WriteLine($"  Observations ({observations.Count}):");
                    foreach (var obs in observations)
                        writer.WriteLine($"    ◇ {obs.ObservationId}  ({obs.SubOutcomes.Count} sub-outcomes)");
                }

                var connected = node.PossibleOutcomes.OfType<NarrationNode>().ToList();
                if (connected.Count > 0)
                {
                    writer.WriteLine($"  Connected ({connected.Count}):");
                    foreach (var child in connected)
                        writer.WriteLine($"    → {child.NodeId}");
                }

                writer.WriteLine();
            }

            writer.WriteLine($"=== Summary ===");
            writer.WriteLine($"Total nodes: {nodeCount}");

            Console.WriteLine($"NarrationGraphFactory: Graph written to {logPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NarrationGraphFactory: Failed to write graph log: {ex.Message}");
        }
    }
}
