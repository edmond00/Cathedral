using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Oldgrowth - Level 9. Ancient forest with massive trees and decay.
/// </summary>
public class OldgrowthNode : NarrationNode
{
    public override string NodeId => "oldgrowth";
    public override string ContextDescription => "exploring the oldgrowth";
    public override string TransitionDescription => "enter the oldgrowth";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "ancient", "giant", "dead", "decaying", "layered", "humus", "massive", "snag", "old", "timeless" };
    
    private static readonly string[] Moods = { "ancient", "primordial", "timeless", "venerable", "aged", "eternal", "prehistoric", "primeval" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} oldgrowth";
    }
}
