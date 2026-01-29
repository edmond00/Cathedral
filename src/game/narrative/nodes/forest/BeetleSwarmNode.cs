using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Beetle Swarm - A congregation of beetles on rotting wood.
/// </summary>
public class BeetleSwarmNode : NarrationNode
{
    public override string NodeId => "beetle_swarm";
    public override string ContextDescription => "observing the beetle swarm";
    public override string TransitionDescription => "approach the beetles";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "black", "shiny", "carapace", "crawling", "clustered", "insects", "hard", "chitinous", "beetles", "swarming" };
    
    private static readonly string[] Moods = { "swarming", "clustered", "busy", "shiny", "teeming", "crowded", "active", "abundant" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} beetle swarm";
    }
}
