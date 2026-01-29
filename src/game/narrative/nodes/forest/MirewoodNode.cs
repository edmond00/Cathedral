using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Mirewood - Level 14. Flooded forest with shallow pools and sedges.
/// </summary>
public class MirewoodNode : NarrationNode
{
    public override string NodeId => "mirewood";
    public override string ContextDescription => "wading through mirewood";
    public override string TransitionDescription => "wade into the mirewood";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "flooded", "pool", "sedge", "spongy", "moss", "rotting", "water", "swamp", "mire", "wet" };
    
    private static readonly string[] Moods = { "waterlogged", "marshy", "boggy", "swampy", "sodden", "saturated", "squelching", "quagmire" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} mirewood";
    }
}
