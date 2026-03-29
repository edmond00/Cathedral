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
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the perfect geometric <web> strung between branches"), KeywordInContext.Parse("the strong sticky <silk> catching the light"), KeywordInContext.Parse("the morning <dew> beading on every strand"), KeywordInContext.Parse("a single fine <thread> vibrating in the breeze") };
    
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
    
    public override List<Item> GetItems() => new() { new SpiderSilk(), new TrappedInsects() };

    public sealed class SpiderSilk : Item
    {
        public override string ItemId => "spider_web_spider_silk";
        public override string DisplayName => "Spider Silk";
        public override string Description => "Strong sticky threads from the web";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the fine <gossamer> strands of the web"), KeywordInContext.Parse("a tiny <filament> of spider silk on the hand") };
    }
    
    public sealed class TrappedInsects : Item
    {
        public override string ItemId => "spider_web_trapped_insects";
        public override string DisplayName => "Trapped Insects";
        public override string Description => "Small insects caught in the sticky web";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a tiny <arthropod> wrapped and waiting"), KeywordInContext.Parse("some small <prey> caught in the sticky web") };
    }
}
