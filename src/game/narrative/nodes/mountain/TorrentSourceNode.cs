using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class TorrentSourceNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 9;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(TorrentChannelNode);
    
    public override string NodeId => "torrent_source";
    public override string ContextDescription => "at the mountain torrent source";
    public override string TransitionDescription => "climb to the torrent source";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a cold mountain <spring> bubbling from the rock"), KeywordInContext.Parse("the beginning of the <torrent> below"), KeywordInContext.Parse("a small <cascade> spilling over smooth stone"), KeywordInContext.Parse("the original <source> of the mountain stream") };
    
    private static readonly string[] Moods = { "rushing", "cascading", "powerful", "pristine" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} torrent source";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"at a {mood} torrent source";
    }
    
    public sealed class SpringPool : Item
    {
        public override string ItemId => "torrent_source_spring_pool";
        public override string DisplayName => "Spring Pool";
        public override string Description => "Small pool where water emerges";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a slow <seep> welling from the rock face"), KeywordInContext.Parse("a small clear <basin> where water collects"), KeywordInContext.Parse("the cold fresh <water> from deep in the stone") };
    }
    
    public sealed class WetRocks : Item
    {
        public override string ItemId => "torrent_source_wet_rocks";
        public override string DisplayName => "Wet Rocks";
        public override string Description => "Water-splashed stones near the spring";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a pale <felsite> stone darkened by spray"), KeywordInContext.Parse("the constant <moisture> clinging to every surface"), KeywordInContext.Parse("the treacherous <slipperiness> of the wet rock") };
    }
    
    public sealed class RiverStone : Item
    {
        public override string ItemId => "torrent_source_river_stone";
        public override string DisplayName => "River Stone";
        public override string Description => "Smooth polished stone collectible from the source";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a rounded <cobble> polished by the source"), KeywordInContext.Parse("the <fluvial> smoothness of this ancient stone"), KeywordInContext.Parse("the mirror <polish> of the water-worn rock") };
    }
}
