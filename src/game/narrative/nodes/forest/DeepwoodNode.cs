using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Deepwood - Level 13. Uniform corridor forest with deep silence.
/// </summary>
public class DeepwoodNode : NarrationNode
{
    public override string NodeId => "deepwood";
    public override string ContextDescription => "walking through deepwood";
    public override string TransitionDescription => "enter the deepwood";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "uniform", "corridor", "sparse", "cold", "moss", "litter", "silent", "isolated", "deep", "still" };
    
    private static readonly string[] Moods = { "silent", "still", "hushed", "quiet", "somber", "serene", "remote", "isolated" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} deepwood";
    }
}
