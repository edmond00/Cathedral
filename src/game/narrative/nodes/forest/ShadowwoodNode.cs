using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Shadowwood - Level 8. Dark woodland with lichen-crusted trees.
/// </summary>
public class ShadowwoodNode : NarrationNode
{
    public override string NodeId => "shadowwood";
    public override string ContextDescription => "moving through shadowwood";
    public override string TransitionDescription => "enter the shadowwood";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "dark", "bare", "lichen", "crusted", "shadowy", "branches", "moss", "gloomy", "hidden", "murky" };
    
    private static readonly string[] Moods = { "gloomy", "darkened", "shadowy", "murky", "obscure", "dim", "somber", "dusky" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} shadowwood";
    }
}
