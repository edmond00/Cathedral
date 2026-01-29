using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative.Nodes;
using Cathedral.Game.Narrative.Nodes.Forest;

namespace Cathedral.Game.Narrative.Factories;

/// <summary>
/// Comprehensive procedural graph generator for forest locations.
/// Samples 5-7 areas from 15 possible forest types, connects them by level adjacency,
/// adds 2-5 transversal features for cross-connections, and populates with child nodes.
/// Each forest is unique and deterministic based on locationId.
/// </summary>
public class ForestGraphFactory : NarrationGraphFactory
{
    private string? _sessionPath;
    
    // All 15 forest area types (Level 1-15)
    private readonly List<Type> _areaTypes = new()
    {
        typeof(OpenWoodlandNode),      // Level 1
        typeof(BrightwoodNode),         // Level 2
        typeof(GreenwoodNode),          // Level 3
        typeof(HighwoodNode),           // Level 4
        typeof(MixedUnderwoodNode),     // Level 5
        typeof(LowwoodNode),            // Level 6
        typeof(DeepCanopyNode),         // Level 7
        typeof(ShadowwoodNode),         // Level 8
        typeof(OldgrowthNode),          // Level 9
        typeof(DenseThicketlandNode),   // Level 10
        typeof(WildwoodNode),           // Level 11
        typeof(RootedForestNode),       // Level 12
        typeof(DeepwoodNode),           // Level 13
        typeof(MirewoodNode),           // Level 14
        typeof(BlackwoodNode)           // Level 15
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
        typeof(PackedEarthTrailNode)
    };
    
    // Child nodes categorized by type
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
        typeof(BareForestFloorNode)
    };
    
    private readonly List<Type> _forageNodes = new()
    {
        typeof(BerryBushNode),
        typeof(MushroomPatchNode),
        typeof(WildStrawberryPatchNode),
        typeof(PineConeClusterNode),
        typeof(WildHerbsNode),
        typeof(SquirrelCacheNode)
    };
    
    private readonly List<Type> _wildlifeNodes = new()
    {
        typeof(BirdsNestNode),
        typeof(AntColonyNode),
        typeof(SpiderWebNode),
        typeof(TreeHollowNode),
        typeof(SnailTrailNode),
        typeof(ButterflyGladeNode),
        typeof(WoodpeckerTreeNode),
        typeof(BeetleSwarmNode),
        typeof(OwlPelletSiteNode),
        typeof(DeerRubNode),
        typeof(FoxDenNode),
        typeof(BeehiveNode),
        typeof(CricketChorusNode)
    };
    
    private readonly List<Type> _featureNodes = new()
    {
        typeof(FallenGiantTrunkNode),
        typeof(TreeSapFlowNode),
        typeof(EarthwormMoundNode),
        typeof(MushroomLogNode)
    };
    
    public ForestGraphFactory(string? sessionPath = null)
    {
        _sessionPath = sessionPath;
    }
    
    public override NarrationNode GenerateGraph(int locationId)
    {
        var rng = CreateSeededRandom(locationId);
        
        // Step 1: Sample 5-7 areas from the 15 possible
        int areaCount = rng.Next(5, 8); // 5, 6, or 7 areas
        var sampledAreaIndices = SampleUniqueIndices(rng, 15, areaCount).OrderBy(x => x).ToList();
        
        Console.WriteLine($"ForestGraphFactory: Sampled {areaCount} areas: {string.Join(", ", sampledAreaIndices.Select(i => i + 1))}");
        
        // Step 2: Create area nodes (sorted by level)
        var areaNodes = new List<NarrationNode>();
        foreach (var index in sampledAreaIndices)
        {
            var node = (NarrationNode)Activator.CreateInstance(_areaTypes[index])!;
            areaNodes.Add(node);
        }
        
        // Step 3: Connect adjacent areas (level-based connections)
        for (int i = 0; i < areaNodes.Count - 1; i++)
        {
            ConnectNodes(areaNodes[i], areaNodes[i + 1]);
            ConnectNodes(areaNodes[i + 1], areaNodes[i]); // Bidirectional
        }
        
        // Step 4: Sample 2-5 transversal features
        int transversalCount = rng.Next(2, 6); // 2 to 5
        var sampledTransversalIndices = SampleUniqueIndices(rng, _transversalTypes.Count, transversalCount);
        
        Console.WriteLine($"ForestGraphFactory: Added {transversalCount} transversal features");
        
        // Step 5: Create transversal nodes and connect random area pairs
        var transversalNodes = new List<NarrationNode>();
        foreach (var index in sampledTransversalIndices)
        {
            var transNode = (NarrationNode)Activator.CreateInstance(_transversalTypes[index])!;
            transversalNodes.Add(transNode);
            
            // Connect to two random areas
            var area1 = areaNodes[rng.Next(areaNodes.Count)];
            var area2 = areaNodes[rng.Next(areaNodes.Count)];
            
            ConnectNodes(area1, transNode);
            ConnectNodes(transNode, area1);
            ConnectNodes(transNode, area2);
            ConnectNodes(area2, transNode);
        }
        
        // Step 6: Add random child nodes to each area
        var allChildTypes = _vegetationNodes.Concat(_forageNodes).Concat(_wildlifeNodes).Concat(_featureNodes).ToList();
        
        foreach (var area in areaNodes)
        {
            int childCount = rng.Next(2, 5); // 2-4 children per area
            var childIndices = SampleUniqueIndices(rng, allChildTypes.Count, childCount);
            
            foreach (var childIndex in childIndices)
            {
                var childNode = (NarrationNode)Activator.CreateInstance(allChildTypes[childIndex])!;
                ConnectNodes(area, childNode);
                ConnectNodes(childNode, area); // Return path
            }
        }
        
        // Step 7: Also add 1-2 child nodes to some transversal features
        foreach (var transNode in transversalNodes)
        {
            if (rng.NextDouble() < 0.5) // 50% chance
            {
                int childCount = rng.Next(1, 3); // 1-2 children
                var childIndices = SampleUniqueIndices(rng, allChildTypes.Count, childCount);
                
                foreach (var childIndex in childIndices)
                {
                    var childNode = (NarrationNode)Activator.CreateInstance(allChildTypes[childIndex])!;
                    ConnectNodes(transNode, childNode);
                    ConnectNodes(childNode, transNode);
                }
            }
        }
        
        // Step 8: Determine entry node (first sampled area 60%, random area 30%, random transversal 10%)
        NarrationNode entryNode;
        double entryRoll = rng.NextDouble();
        if (entryRoll < 0.6)
        {
            entryNode = areaNodes[0]; // First area
        }
        else if (entryRoll < 0.9)
        {
            entryNode = areaNodes[rng.Next(areaNodes.Count)]; // Random area
        }
        else
        {
            entryNode = transversalNodes.Count > 0 ? transversalNodes[rng.Next(transversalNodes.Count)] : areaNodes[0];
        }
        
        // Log the generated graph structure
        WriteGraphToLog(entryNode, locationId, _sessionPath);
        
        Console.WriteLine($"ForestGraphFactory: Generated forest graph for location {locationId} with entry '{entryNode.NodeId}'");
        
        return entryNode;
    }
    
    /// <summary>
    /// Samples unique random indices from [0, maxIndex).
    /// </summary>
    private List<int> SampleUniqueIndices(Random rng, int maxIndex, int count)
    {
        var indices = Enumerable.Range(0, maxIndex).ToList();
        var result = new List<int>();
        
        for (int i = 0; i < count && indices.Count > 0; i++)
        {
            int randomIndex = rng.Next(indices.Count);
            result.Add(indices[randomIndex]);
            indices.RemoveAt(randomIndex);
        }
        
        return result;
    }
}
