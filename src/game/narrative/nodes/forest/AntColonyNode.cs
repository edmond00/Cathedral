using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Ant Colony - A busy anthill with foraging ants.
/// </summary>
public class AntColonyNode : NarrationNode
{
    public override string NodeId => "ant_colony";
    public override string ContextDescription => "watching the ant colony";
    public override string TransitionDescription => "observe the ants";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "mound", "busy", "crawling", "trails", "workers", "earth", "organized", "black", "swarming", "insects" };
    
    private static readonly string[] Moods = { "busy", "industrious", "swarming", "organized", "active", "thriving", "teeming", "bustling" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} ant colony";
    }
    
    public sealed class AntEggs : Item
    {
        public override string ItemId => "ant_colony_ant_eggs";
        public override string DisplayName => "Ant Eggs";
        public override string Description => "Tiny pale eggs from the ant colony";
        public override List<string> OutcomeKeywords => new() { "eggs", "tiny", "pale", "white", "larvae", "cluster", "small", "oval", "delicate", "brood" };
    }
    
    public sealed class ForamicAcid : Item
    {
        public override string ItemId => "ant_colony_foramic_acid";
        public override string DisplayName => "Foramic Acid";
        public override string Description => "Pungent defensive secretion from worker ants";
        public override List<string> OutcomeKeywords => new() { "acid", "pungent", "sharp", "chemical", "defensive", "bitter", "stinging", "irritating", "secretion", "ant" };
    }
}
