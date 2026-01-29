using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Leaf-Cleared Path - A transversal feature maintained by wind or passage.
/// </summary>
public class LeafClearedPathNode : NarrationNode
{
    public override string NodeId => "leaf_cleared_path";
    public override string ContextDescription => "walking the leaf-cleared path";
    public override string TransitionDescription => "take the cleared path";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "swept", "clear", "clean", "wind", "bare", "tidy", "maintained", "path", "trail", "open" };
    
    private static readonly string[] Moods = { "swept", "clear", "tidy", "clean", "maintained", "neat", "orderly", "pristine" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} leaf-cleared path";
    }
}
