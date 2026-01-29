using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Deep Canopy - Level 7. Closed-crown forest with filtered light.
/// </summary>
public class DeepCanopyNode : NarrationNode
{
    public override string NodeId => "deep_canopy";
    public override string ContextDescription => "walking beneath the deep canopy";
    public override string TransitionDescription => "enter the deep canopy";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "closed", "crown", "roots", "stone", "shade", "filtered", "shafts", "dim", "trunk", "carpet" };
    
    private static readonly string[] Moods = { "sheltered", "enclosed", "shadowed", "filtered", "dim", "protected", "covered", "roofed" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} deep canopy";
    }
}
