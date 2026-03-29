using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Forest Stream - A transversal feature connecting areas with flowing water.
/// </summary>
public class ForestStreamNode : NarrationNode
{
    public override string NodeId => "forest_stream";
    public override string ContextDescription => "following the forest stream";
    public override string TransitionDescription => "follow the stream";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the clear cold <water> running over stones"), KeywordInContext.Parse("the gentle <current> pushing against the feet"), KeywordInContext.Parse("a smooth <stone> beneath the flowing water"), KeywordInContext.Parse("the steady <babbling> of the stream over rocks") };
    
    private static readonly string[] Moods = { "babbling", "rushing", "trickling", "murmuring", "gurgling", "flowing", "cascading", "meandering" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} forest stream";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"following a {mood} forest stream";
    }
    
    public override List<Item> GetItems() => new() { new StreamWater(), new WatersmoothedPebbles() };

    public sealed class StreamWater : Item
    {
        public override string ItemId => "stream_water";
        public override string DisplayName => "Stream Water";
        public override string Description => "Clear, cold water flowing from upstream";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the clear <purity> of cold upstream water"), KeywordInContext.Parse("a <freshness> in the air near the stream") };
    }
    
    public sealed class WatersmoothedPebbles : Item
    {
        public override string ItemId => "forest_stream_water_smoothed_pebbles";
        public override string DisplayName => "Water-smoothed Pebbles";
        public override string Description => "Smooth pebbles from the stream bed";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a rounded <cobble> lifted from the stream bed"), KeywordInContext.Parse("a water-polished <stone> smooth to the touch") };
    }
}
