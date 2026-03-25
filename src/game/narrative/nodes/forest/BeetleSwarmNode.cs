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
    
    public override List<string> NodeKeywords => new() { "carapace", "beetles", "chitin", "swarm" };
    
    private static readonly string[] Moods = { "swarming", "clustered", "busy", "shiny", "teeming", "crowded", "active", "abundant" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} beetle swarm";
    }
    
    public sealed class BeetleCarapace : Item
    {
        public override string ItemId => "beetle_swarm_beetle_carapace";
        public override string DisplayName => "Beetle Carapace";
        public override string Description => "Hard black shell from a dead beetle";
        public override List<string> OutcomeKeywords => new() { "carapace", "exoskeleton", "chitin", "shell" };
    }
    
    public sealed class BeetleFrass : Item
    {
        public override string ItemId => "beetle_swarm_beetle_frass";
        public override string DisplayName => "Beetle Frass";
        public override string Description => "Wood powder from beetle boring activity";
        public override List<string> OutcomeKeywords => new() { "frass", "sawdust", "debris" };
    }
}
