using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative.Nodes.Plain;
using Cathedral.Game.Narrative.Observations.Plain;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Factories;

/// <summary>
/// Builds a procedural plain narration graph.
///
/// Graph structure:
///   - Randomly samples 2-4 of the 4 subarea types (Meadow, Hill, Valley, Grassland).
///   - Connects subareas linearly with bidirectional edges.
///   - Populates each subarea with 1-3 randomly sampled observations from the 5 plain types.
///
/// NPCs:
///   - Fox      — dawn and dusk (crepuscular)
///   - Stray Dog — always present at their node (feral, no schedule preference)
///   - Stray Cat — morning and evening only (avoids midday heat)
///   - Black Bear — morning, noon, afternoon (diurnal forager)
/// </summary>
public class PlainGraphFactory : NarrationGraphFactory
{
    private readonly List<Type> _subareaTypes = new()
    {
        typeof(MeadowNode),
        typeof(HillNode),
        typeof(ValleyNode),
        typeof(GrasslandNode),
    };

    private readonly List<Type> _observationTypes = new()
    {
        typeof(AppleTreeObservation),
        typeof(PineTreeObservation),
        typeof(BushObservation),
        typeof(FlowerPatchObservation),
        typeof(BoulderObservation),
    };

    public PlainGraphFactory(string? sessionPath = null) : base(sessionPath) { }

    protected override NarrationNode BuildNodes(Random rng, int locationId)
    {
        // Step 1: Sample 2-4 subareas
        int areaCount = rng.Next(2, 5);
        var sampledIndices = SampleUniqueIndices(rng, _subareaTypes.Count, areaCount);
        Console.WriteLine($"PlainGraphFactory: Sampled {areaCount} subareas");

        var subareas = sampledIndices
            .Select(i => (NarrationNode)Activator.CreateInstance(_subareaTypes[i])!)
            .ToList();

        // Step 2: Connect subareas linearly (bidirectional)
        for (int i = 0; i < subareas.Count - 1; i++)
        {
            ConnectNodes(subareas[i], subareas[i + 1]);
            ConnectNodes(subareas[i + 1], subareas[i]);
        }

        // Step 3: Add 1-3 observations per subarea (sampled from shared pool, repeats allowed)
        foreach (var area in subareas)
        {
            int obsCount = rng.Next(1, 4);
            foreach (var obsIndex in SampleUniqueIndices(rng, _observationTypes.Count, obsCount))
            {
                var obs = (ObservationObject)Activator.CreateInstance(_observationTypes[obsIndex])!;
                area.PossibleOutcomes.Add(obs);
            }
        }

        NarrationNode entryNode = subareas[0];
        Console.WriteLine($"PlainGraphFactory: Built plain graph (entry '{entryNode.NodeId}')");
        return entryNode;
    }

    // Possible plain encounters with their spawn chance
    private static readonly List<(NpcArchetype Archetype, float SpawnChance)> _plainEncounters = new()
    {
        (new FoxArchetype(),      0.40f),
        (new StrayDogArchetype(), 0.30f),
        (new StrayCatArchetype(), 0.25f),
        (new BlackBearArchetype(), 0.15f),
    };

    protected override List<GraphNpc> BuildNpcs(
        Random rng,
        IReadOnlyDictionary<string, NarrationNode> allNodes,
        int locationId)
    {
        var npcs = new List<GraphNpc>();
        var nodeList = allNodes.Values.ToList();

        // Roll each possible encounter type against a randomly chosen subarea
        foreach (var (archetype, spawnChance) in _plainEncounters)
        {
            if (rng.NextDouble() > spawnChance) continue;
            var targetNode = nodeList[rng.Next(nodeList.Count)];
            var entity = archetype.Spawn(rng, targetNode.ContextDescription);
            var schedule = BuildPlainSchedule(archetype.ArchetypeId, targetNode.NodeId);
            npcs.Add(new GraphNpc(entity, schedule));
        }

        return npcs;
    }

    /// <summary>
    /// Per-archetype time-of-day schedules for the plain biome.
    /// </summary>
    private static NpcSchedule BuildPlainSchedule(string archetypeId, string nodeId)
        => archetypeId switch
        {
            // Fox: crepuscular — active at dawn and dusk
            "fox"        => NpcSchedule.OnlyDuring(nodeId,
                                TimePeriod.Dawn, TimePeriod.Evening),

            // Black Bear: diurnal forager
            "black_bear" => NpcSchedule.OnlyDuring(nodeId,
                                TimePeriod.Morning, TimePeriod.Noon, TimePeriod.Afternoon),

            // Stray Cat: avoids midday heat
            "stray_cat"  => NpcSchedule.OnlyDuring(nodeId,
                                TimePeriod.Morning, TimePeriod.Evening),

            // Stray Dog: always present
            _            => NpcSchedule.Always(nodeId),
        };

    private static List<int> SampleUniqueIndices(Random rng, int maxIndex, int count)
    {
        var indices = Enumerable.Range(0, maxIndex).ToList();
        var result = new List<int>();
        for (int i = 0; i < count && indices.Count > 0; i++)
        {
            int r = rng.Next(indices.Count);
            result.Add(indices[r]);
            indices.RemoveAt(r);
        }
        return result;
    }
}
