using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Greenwood - Level 3. Mixed hardwood forest with hazel and fungal rings.
/// </summary>
public class GreenwoodNode : NarrationNode
{
    public override string NodeId => "greenwood";
    public override string ContextDescription => "walking through greenwood";
    public override string TransitionDescription => "enter the greenwood";
    public override bool IsEntryNode => true;
    
    public override List<string> NodeKeywords => new() { "verdant", "mixed", "hazel", "fungal", "lush", "leafy", "mossy", "green", "dense", "alive" };
    
    private static readonly string[] Moods = { "thriving", "lush", "verdant", "rich", "vibrant", "living", "dense", "flourishing" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} greenwood";
    }
}
