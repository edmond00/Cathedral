using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Spider Web - An intricate web strung between branches.
/// </summary>
public class SpiderWebNode : NarrationNode
{
    public override string NodeId => "spider_web";
    public override string ContextDescription => "examining the spider web";
    public override string TransitionDescription => "inspect the web";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "silk", "threads", "dew", "sticky", "pattern", "radial", "spiral", "delicate", "glistening", "web" };
    
    private static readonly string[] Moods = { "delicate", "glistening", "intricate", "perfect", "dew-covered", "shimmering", "geometric", "pristine" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} spider web";
    }
}
