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
}
