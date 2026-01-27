using System;
using Cathedral.Game.Narrative.Nodes;

namespace Cathedral.Game.Narrative.Factories;

/// <summary>
/// Procedural graph generator for forest locations.
/// Creates randomized forest narrative graphs with:
/// - Random entry node (70% clearing, 30% stream)
/// - Always: clearing ↔ stream bidirectional connection
/// - Always: berry bush connected to clearing
/// - 40% chance: mushroom patch connected to clearing
/// - Always: caught trout connected to stream
/// - All secondary nodes connect back to clearing
/// </summary>
public class ForestGraphFactory : NarrationGraphFactory
{
    private string? _sessionPath;
    
    public ForestGraphFactory(string? sessionPath = null)
    {
        _sessionPath = sessionPath;
    }
    
    public override NarrationNode GenerateGraph(int locationId)
    {
        var rng = CreateSeededRandom(locationId);
        
        // Create all nodes
        var clearing = CreateNode<ClearingNode>();
        var stream = CreateNode<StreamNode>();
        var berryBush = CreateNode<BerryBushNode>();
        var caughtTrout = CreateNode<CaughtTroutNode>();
        
        // Core connections: clearing ↔ stream (bidirectional)
        ConnectNodes(clearing, stream);
        ConnectNodes(stream, clearing);
        
        // Always connect berry bush to clearing
        ConnectNodes(clearing, berryBush);
        ConnectNodes(berryBush, clearing);
        
        // Always connect caught trout to stream
        ConnectNodes(stream, caughtTrout);
        ConnectNodes(caughtTrout, stream);
        
        // Optional: mushroom patch (40% chance)
        if (rng.NextDouble() < 0.4)
        {
            var mushroomPatch = CreateNode<MushroomPatchNode>();
            ConnectNodes(clearing, mushroomPatch);
            ConnectNodes(mushroomPatch, clearing);
        }
        
        // Determine entry node: 70% clearing, 30% stream
        NarrationNode entryNode = rng.NextDouble() < 0.7 ? clearing : stream;
        
        // Log the generated graph structure
        WriteGraphToLog(entryNode, locationId, _sessionPath);
        
        Console.WriteLine($"ForestGraphFactory: Generated graph for location {locationId} with entry node '{entryNode.NodeId}'");
        
        return entryNode;
    }
}
