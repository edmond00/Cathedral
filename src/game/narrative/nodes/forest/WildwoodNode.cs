using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Wildwood - Level 11. Chaotic mixed-age forest with uprooted trees.
/// </summary>
public class WildwoodNode : NarrationNode
{
    public override string NodeId => "wildwood";
    public override string ContextDescription => "navigating the wildwood";
    public override string TransitionDescription => "enter the wildwood";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "mixed-age", "uprooted", "regrowth", "uneven", "competing", "chaotic", "wild", "tangled", "disrupted", "rough" };
    
    private static readonly string[] Moods = { "chaotic", "wild", "untamed", "disordered", "turbulent", "rugged", "rough", "feral" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} wildwood";
    }
}
