using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Lowwood - Level 6. Moist woodland with alder and fungal growth.
/// </summary>
public class LowwoodNode : NarrationNode
{
    public override string NodeId => "lowwood";
    public override string ContextDescription => "treading through lowwood";
    public override string TransitionDescription => "descend into the lowwood";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "moist", "alder", "damp", "fungal", "reeds", "puddles", "wet", "soggy", "low", "depression" };
    
    private static readonly string[] Moods = { "damp", "soggy", "humid", "waterlogged", "misty", "moist", "dripping", "wet" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} lowwood";
    }
}
