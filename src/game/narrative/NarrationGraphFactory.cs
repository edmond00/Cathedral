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
/// Subclass responsibilities:
///   <see cref="BuildNodes"/>  — build the NarrationNode network and return the entry node.
///   <see cref="BuildNpcs"/>   — (optional override) produce GraphNpc list with schedules.
///
/// The base <see cref="BuildNpcs"/> reads <c>PossibleEncounters</c> from every node and
/// <c>AssociatedEncounters</c> from every ObservationObject, rolls RNG for graph inclusion,
/// and assigns a simple <see cref="NpcSchedule.Always"/> schedule by default.
/// Override in a subclass to add time-of-day movement.
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
        var npcs     = BuildNpcs(rng, allNodes, locationId);
        var graph    = new NarrationGraph(entry, npcs, allNodes);
        WriteGraphToLog(graph, locationId);
        return graph;
    }

    // ── Abstract / virtual hooks ──────────────────────────────────────────────

    /// <summary>
    /// Build the NarrationNode network (area nodes, transversals, observations, etc.)
    /// and return the entry node.  PossibleOutcomes must be populated before returning.
    /// </summary>
    protected abstract NarrationNode BuildNodes(Random rng, int locationId);

    /// <summary>
    /// Build the GraphNpc list for this graph.
    /// The default implementation reads <c>PossibleEncounters</c> on every node and
    /// <c>AssociatedEncounters</c> on every ObservationObject, rolls RNG against
    /// SpawnChance (now treated as graph inclusion probability), spawns the NPC once,
    /// and assigns <see cref="NpcSchedule.Always"/> at that node.
    ///
    /// Override to apply richer per-archetype schedules or roaming patterns.
    /// </summary>
    protected virtual List<GraphNpc> BuildNpcs(
        Random rng,
        IReadOnlyDictionary<string, NarrationNode> allNodes,
        int locationId)
    {
        var npcs = new List<GraphNpc>();

        foreach (var (nodeId, node) in allNodes)
        {
            // Node-level encounter slots
            foreach (var slot in node.PossibleEncounters)
                TryAddNpc(npcs, slot, nodeId, rng, node.ContextDescription);

            // ObservationObject associated encounter slots
            foreach (var obs in node.PossibleOutcomes.OfType<ObservationObject>())
                foreach (var slot in obs.AssociatedEncounters)
                    TryAddNpc(npcs, slot, nodeId, rng, node.ContextDescription);
        }

        return npcs;
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    protected Random CreateSeededRandom(int locationId)
        => new Random(locationId);

    protected void ConnectNodes(NarrationNode from, NarrationNode to)
    {
        if (from == to) return;
        if (!from.PossibleOutcomes.Contains(to))
            from.PossibleOutcomes.Add(to);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void TryAddNpc(
        List<GraphNpc> npcs,
        NpcEncounterSlot slot,
        string nodeId,
        Random rng,
        string nodeContext)
    {
        for (int i = 0; i < slot.MaxCount; i++)
        {
            if (rng.NextDouble() > slot.SpawnChance) continue;  // graph inclusion roll
            var entity   = slot.Archetype.Spawn(rng, nodeContext);
            var schedule = NpcSchedule.Always(nodeId);
            npcs.Add(new GraphNpc(entity, schedule));
        }
    }

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

                var items = node.GetAvailableItems();
                if (items.Count > 0)
                {
                    writer.WriteLine($"  Items ({items.Count}):");
                    foreach (var item in items)
                        writer.WriteLine($"    - {item.DisplayName} ({item.ItemId})");
                }

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

            // NPCs
            writer.WriteLine($"=== NPCs ({graph.Npcs.Count}) ===");
            foreach (var gnpc in graph.Npcs)
            {
                writer.WriteLine($"NPC: {gnpc.Entity.DisplayName}  [{gnpc.Entity.Archetype.ArchetypeId}]");
                writer.WriteLine($"  Persistent: {gnpc.Entity.IsPersistent}  CanSpeak: {gnpc.Entity.CanSpeak}");
                writer.WriteLine($"  Schedule:");
                foreach (TimePeriod p in Enum.GetValues(typeof(TimePeriod)))
                {
                    var nodeId = gnpc.Schedule.GetNodeId(p);
                    writer.WriteLine($"    {p,-10} → {nodeId ?? "(absent)"}");
                }
                writer.WriteLine();
            }

            writer.WriteLine($"=== Summary ===");
            writer.WriteLine($"Total nodes: {nodeCount}");
            writer.WriteLine($"Total NPCs:  {graph.Npcs.Count}");

            Console.WriteLine($"NarrationGraphFactory: Graph written to {logPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NarrationGraphFactory: Failed to write graph log: {ex.Message}");
        }
    }
}
