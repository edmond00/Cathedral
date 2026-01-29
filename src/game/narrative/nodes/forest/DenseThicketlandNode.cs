using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Dense Thicketland - Level 10. Impenetrable shrub walls and vine growth.
/// </summary>
public class DenseThicketlandNode : NarrationNode
{
    public override string NodeId => "dense_thicketland";
    public override string ContextDescription => "pushing through dense thicketland";
    public override string TransitionDescription => "force into the thicketland";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "interlocked", "shrub", "wall", "vine", "draped", "thorns", "impenetrable", "tangled", "pocket", "dense" };
    
    private static readonly string[] Moods = { "impenetrable", "tangled", "maze-like", "interwoven", "cluttered", "blocked", "choked", "knotted" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} dense thicketland";
    }
}
