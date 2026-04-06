using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative.Nodes.Forest;
using Cathedral.Game.Narrative.Observations.Forest;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative.Factories;

/// <summary>
/// Builds a procedural forest narration graph.
/// Samples 5-7 areas from 15 possible forest types, connects them by level adjacency,
/// adds 2-5 transversal features for cross-connections, and populates each area with
/// child nodes / observations.
///
/// NPCs are assigned archetype-specific day schedules:
///   Wolf  — present all periods except Noon (nocturnal hunter)
///   Bear  — present only Morning / Noon / Afternoon (diurnal)
///   Boar  — present Dawn / Morning / Afternoon / Evening (avoids heat and night)
///   Druid — present Dawn / Morning / Evening (liminal hours)
///   Hermit / others — always present at their node
/// </summary>
public class ForestGraphFactory : NarrationGraphFactory
{
    // All 15 forest area types (Level 1-15)
    private readonly List<Type> _areaTypes = new()
    {
        typeof(OpenWoodlandNode),
        typeof(BrightwoodNode),
        typeof(GreenwoodNode),
        typeof(HighwoodNode),
        typeof(MixedUnderwoodNode),
        typeof(LowwoodNode),
        typeof(DeepCanopyNode),
        typeof(ShadowwoodNode),
        typeof(OldgrowthNode),
        typeof(DenseThicketlandNode),
        typeof(WildwoodNode),
        typeof(RootedForestNode),
        typeof(DeepwoodNode),
        typeof(MirewoodNode),
        typeof(BlackwoodNode),
    };

    // All 11 transversal features
    private readonly List<Type> _transversalTypes = new()
    {
        typeof(ForestStreamNode),
        typeof(DryCreekBedNode),
        typeof(AnimalTrackNode),
        typeof(RootPathNode),
        typeof(FallenLogLineNode),
        typeof(DrainageHollowNode),
        typeof(CanopyGapLineNode),
        typeof(BoulderChainNode),
        typeof(WornGroundPathNode),
        typeof(LeafClearedPathNode),
        typeof(PackedEarthTrailNode),
    };

    // Child nodes categorised by type
    private readonly List<Type> _vegetationNodes = new()
    {
        typeof(IsolatedOakNode),
        typeof(HawthornClusterNode),
        typeof(WildflowerPatchNode),
        typeof(FernGladeNode),
        typeof(MossyStoneOutcropNode),
        typeof(HazelUndergrowthNode),
        typeof(FungalRingNode),
        typeof(BrambleRunNode),
        typeof(AlderGroveNode),
        typeof(LowShrubBeltNode),
        typeof(GrassClearingNode),
        typeof(BeechStandNode),
        typeof(YoungMapleGroupNode),
        typeof(IvyCladTrunkNode),
        typeof(LeafLitterHollowNode),
        typeof(LichenBarkNode),
        typeof(ExposedRootPlateNode),
        typeof(SaplingThicketNode),
        typeof(TallFernStandNode),
        typeof(ReededDepressionNode),
        typeof(SeasonalPuddleNode),
        typeof(ShadePlantPatchNode),
        typeof(TrunkClusterNode),
        typeof(LowMossBedNode),
        typeof(HiddenBranchTangleNode),
        typeof(AncientTreeGiantNode),
        typeof(DeepHumusBasinNode),
        typeof(VineDrapedGrowthNode),
        typeof(ShrubIslandNode),
        typeof(RegrowthThicketNode),
        typeof(UprootedTreeNode),
        typeof(RootArchNode),
        typeof(RootWebNode),
        typeof(DeepLeafLitterNode),
        typeof(IsolatedPlantClusterNode),
        typeof(SedgePatchNode),
        typeof(SpongyMossMatNode),
        typeof(DeadwoodHeapNode),
        typeof(BareForestFloorNode),
    };

    private readonly List<Type> _forageNodes = new()
    {
        typeof(WildStrawberryPatchNode),
        typeof(PineConeClusterNode),
        typeof(WildHerbsNode),
        typeof(SquirrelCacheObservation),
    };

    private readonly List<Type> _wildlifeNodes = new()
    {
        typeof(BirdsNestObservation),
        typeof(AntColonyObservation),
        typeof(SpiderWebNode),
        typeof(TreeHollowNode),
        typeof(SnailTrailObservation),
        typeof(ButterflyGladeNode),
        typeof(WoodpeckerTreeNode),
        typeof(BeetleSwarmNode),
        typeof(OwlPelletSiteObservation),
        typeof(DeerRubNode),
        typeof(FoxDenObservation),
        typeof(BeehiveNode),
        typeof(CricketChorusNode),
    };

    private readonly List<Type> _featureNodes = new()
    {
        typeof(FallenGiantTrunkNode),
        typeof(TreeSapFlowNode),
        typeof(EarthwormMoundObservation),
        typeof(MushroomLogNode),
    };

    public ForestGraphFactory(string? sessionPath = null) : base(sessionPath) { }

    // ── BuildNodes ────────────────────────────────────────────────────────────

    protected override NarrationNode BuildNodes(Random rng, int locationId)
    {
        // Step 1: Sample 5-7 areas
        int areaCount = rng.Next(5, 8);
        var sampledAreaIndices = SampleUniqueIndices(rng, 15, areaCount).OrderBy(x => x).ToList();
        Console.WriteLine($"ForestGraphFactory: Sampled {areaCount} areas: {string.Join(", ", sampledAreaIndices.Select(i => i + 1))}");

        // Step 2: Create area nodes
        var areaNodes = sampledAreaIndices
            .Select(i => (NarrationNode)Activator.CreateInstance(_areaTypes[i])!)
            .ToList();

        // Step 3: Connect adjacent areas (bidirectional)
        for (int i = 0; i < areaNodes.Count - 1; i++)
        {
            ConnectNodes(areaNodes[i], areaNodes[i + 1]);
            ConnectNodes(areaNodes[i + 1], areaNodes[i]);
        }

        // Step 4: Sample 2-5 transversal features
        int transversalCount = rng.Next(2, 6);
        var sampledTransversalIndices = SampleUniqueIndices(rng, _transversalTypes.Count, transversalCount);
        Console.WriteLine($"ForestGraphFactory: Added {transversalCount} transversal features");

        // Step 5: Create transversal nodes and connect to random area pairs
        var transversalNodes = new List<NarrationNode>();
        foreach (var index in sampledTransversalIndices)
        {
            var transNode = (NarrationNode)Activator.CreateInstance(_transversalTypes[index])!;
            transversalNodes.Add(transNode);

            var area1 = areaNodes[rng.Next(areaNodes.Count)];
            var area2 = areaNodes[rng.Next(areaNodes.Count)];
            ConnectNodes(area1, transNode);
            ConnectNodes(transNode, area1);
            ConnectNodes(transNode, area2);
            ConnectNodes(area2, transNode);
        }

        // Step 6: Add child nodes/observations to each area
        var allChildTypes = _vegetationNodes
            .Concat(_forageNodes)
            .Concat(_wildlifeNodes)
            .Concat(_featureNodes)
            .ToList();

        foreach (var area in areaNodes)
        {
            int childCount = rng.Next(2, 5);
            foreach (var childIndex in SampleUniqueIndices(rng, allChildTypes.Count, childCount))
                AttachChildToParent(area, allChildTypes[childIndex]);
        }

        // Step 7: Possibly add 1-2 children to transversal nodes
        foreach (var transNode in transversalNodes)
        {
            if (rng.NextDouble() < 0.5)
                foreach (var childIndex in SampleUniqueIndices(rng, allChildTypes.Count, rng.Next(1, 3)))
                    AttachChildToParent(transNode, allChildTypes[childIndex]);
        }

        // Step 8: Choose entry node
        NarrationNode entryNode;
        double entryRoll = rng.NextDouble();
        if (entryRoll < 0.6)
            entryNode = areaNodes[0];
        else if (entryRoll < 0.9)
            entryNode = areaNodes[rng.Next(areaNodes.Count)];
        else
            entryNode = transversalNodes.Count > 0 ? transversalNodes[rng.Next(transversalNodes.Count)] : areaNodes[0];

        Console.WriteLine($"ForestGraphFactory: Built forest graph for location (entry '{entryNode.NodeId}')");
        return entryNode;
    }

    // ── BuildNpcs — archetype-specific schedules ──────────────────────────────

    protected override List<GraphNpc> BuildNpcs(
        Random rng,
        IReadOnlyDictionary<string, NarrationNode> allNodes,
        int locationId)
    {
        var npcs = new List<GraphNpc>();

        foreach (var (nodeId, node) in allNodes)
        {
            foreach (var slot in node.PossibleEncounters)
                TryAddScheduledNpc(npcs, slot, nodeId, rng, node.ContextDescription);

            foreach (var obs in node.PossibleOutcomes.OfType<ObservationObject>())
                foreach (var slot in obs.AssociatedEncounters)
                    TryAddScheduledNpc(npcs, slot, nodeId, rng, node.ContextDescription);
        }

        return npcs;
    }

    private static void TryAddScheduledNpc(
        List<GraphNpc> npcs,
        NpcEncounterSlot slot,
        string nodeId,
        Random rng,
        string nodeContext)
    {
        for (int i = 0; i < slot.MaxCount; i++)
        {
            if (rng.NextDouble() > slot.SpawnChance) continue;
            var entity   = slot.Archetype.Spawn(rng, nodeContext);
            var schedule = BuildForestSchedule(slot.Archetype.ArchetypeId, nodeId);
            npcs.Add(new GraphNpc(entity, schedule));
        }
    }

    /// <summary>
    /// Per-archetype time-of-day schedules for a forest biome.
    /// </summary>
    private static NpcSchedule BuildForestSchedule(string archetypeId, string nodeId)
        => archetypeId switch
        {
            // Wolf: active all day except the bright midday hours
            "wolf"   => NpcSchedule.ExceptDuring(nodeId, TimePeriod.Noon),

            // Bear: diurnal, forages in daylight
            "bear"   => NpcSchedule.OnlyDuring(nodeId,
                            TimePeriod.Morning, TimePeriod.Noon, TimePeriod.Afternoon),

            // Boar: active dawn to dusk, rests at night
            "boar"   => NpcSchedule.OnlyDuring(nodeId,
                            TimePeriod.Dawn, TimePeriod.Morning,
                            TimePeriod.Afternoon, TimePeriod.Evening),

            // Druid: liminal hours — dawn rites and evening meditation
            "druid"  => NpcSchedule.OnlyDuring(nodeId,
                            TimePeriod.Dawn, TimePeriod.Morning, TimePeriod.Evening),

            // Hermit, savage, and unknown archetypes: always at their node
            _        => NpcSchedule.Always(nodeId),
        };

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Attaches a child type (NarrationNode or ObservationObject) to a parent node.
    /// NarrationNodes get bidirectional edges; ObservationObjects are added directly to
    /// PossibleOutcomes without a back-edge (they are entered via keyword click, not traversal).
    /// </summary>
    private void AttachChildToParent(NarrationNode parent, Type childType)
    {
        if (typeof(ObservationObject).IsAssignableFrom(childType))
        {
            var obs = (ObservationObject)Activator.CreateInstance(childType)!;
            parent.PossibleOutcomes.Add(obs);
            // AssociatedEncounters are read by BuildNpcs directly from PossibleOutcomes —
            // no need to propagate them to the parent node anymore.
        }
        else
        {
            var childNode = (NarrationNode)Activator.CreateInstance(childType)!;
            ConnectNodes(parent, childNode);
            ConnectNodes(childNode, parent);
        }
    }

    private static List<int> SampleUniqueIndices(Random rng, int maxIndex, int count)
    {
        var indices = Enumerable.Range(0, maxIndex).ToList();
        var result  = new List<int>();
        for (int i = 0; i < count && indices.Count > 0; i++)
        {
            int r = rng.Next(indices.Count);
            result.Add(indices[r]);
            indices.RemoveAt(r);
        }
        return result;
    }
}
