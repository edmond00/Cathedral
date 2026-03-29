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
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a cluster of ripe <hazel> nuts hanging low"), KeywordInContext.Parse("some mottled <fungus> on a fallen log"), KeywordInContext.Parse("the deep <moss> covering stone and root alike"), KeywordInContext.Parse("a sense of <vitality> in the thriving undergrowth") };
    
    private static readonly string[] Moods = { "thriving", "lush", "verdant", "rich", "vibrant", "living", "dense", "flourishing" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} greenwood";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"walking through a {mood} greenwood";
    }
    
    public override List<Item> GetItems() => new() { new HazelNuts() };

    public sealed class HazelNuts : Item
    {
        public override string ItemId => "greenwood_hazel_nuts";
        public override string DisplayName => "Hazel Nuts";
        public override string Description => "Small brown nuts from greenwood hazel trees";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a hard brown <shell> around the hazelnut"), KeywordInContext.Parse("a leafy <corylus> husk protecting the kernel"), KeywordInContext.Parse("a <cluster> of nuts still on the branch") };
    }
}
