using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Butterfly Glade - A sunny spot where butterflies gather.
/// </summary>
public class ButterflyGladeNode : NarrationNode
{
    public override string NodeId => "butterfly_glade";
    public override string ContextDescription => "watching butterflies in the glade";
    public override string TransitionDescription => "enter the butterfly glade";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "wings", "colorful", "fluttering", "dancing", "bright", "flowers", "nectar", "delicate", "aerial", "insects" };
    
    private static readonly string[] Moods = { "colorful", "dancing", "fluttering", "bright", "lively", "vibrant", "magical", "enchanting" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} butterfly glade";
    }
}
