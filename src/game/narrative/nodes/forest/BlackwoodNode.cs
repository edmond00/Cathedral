using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Blackwood - Level 15. Lightless forest with dense trunk walls and decay.
/// </summary>
public class BlackwoodNode : NarrationNode
{
    public override string NodeId => "blackwood";
    public override string ContextDescription => "feeling through blackwood";
    public override string TransitionDescription => "enter the blackwood";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "dense", "wall", "deadwood", "heap", "bare", "damp", "lightless", "dark", "black", "collapsed" };
    
    private static readonly string[] Moods = { "lightless", "pitch-dark", "oppressive", "suffocating", "impenetrable", "black", "void-like", "abyssal" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} blackwood";
    }
}
