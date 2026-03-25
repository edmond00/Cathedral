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
    
    public override List<string> NodeKeywords => new() { "web", "silk", "dew", "thread" };
    
    private static readonly string[] Moods = { "delicate", "glistening", "intricate", "perfect", "dew-covered", "shimmering", "geometric", "pristine" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} spider web";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} spider web";
    }
    
    public sealed class SpiderSilk : Item
    {
        public override string ItemId => "spider_web_spider_silk";
        public override string DisplayName => "Spider Silk";
        public override string Description => "Strong sticky threads from the web";
        public override List<string> OutcomeKeywords => new() { "silk", "gossamer", "thread", "protein" };
    }
    
    public sealed class TrappedInsects : Item
    {
        public override string ItemId => "spider_web_trapped_insects";
        public override string DisplayName => "Trapped Insects";
        public override string Description => "Small insects caught in the sticky web";
        public override List<string> OutcomeKeywords => new() { "insect", "prey", "trap" };
    }
}
