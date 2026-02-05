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
    
    public override List<string> NodeKeywords => new() { "torrent", "source", "spring", "water", "origin", "flowing", "fresh", "cold", "bubbling", "cascade" };
    
    private static readonly string[] Moods = { "rushing", "cascading", "powerful", "pristine" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} torrent source";
    }
    
    public sealed class SpringPool : Item
    {
        public override string ItemId => "torrent_source_spring_pool";
        public override string DisplayName => "Spring Pool";
        public override string Description => "Small pool where water emerges";
        public override List<string> OutcomeKeywords => new() { "spring", "pool", "water", "source", "clear", "cold", "bubbling", "fresh", "emerging", "pristine" };
    }
    
    public sealed class WetRocks : Item
    {
        public override string ItemId => "torrent_source_wet_rocks";
        public override string DisplayName => "Wet Rocks";
        public override string Description => "Water-splashed stones near the spring";
        public override List<string> OutcomeKeywords => new() { "wet", "rocks", "splashed", "slippery", "dark", "shining", "moist", "damp", "glistening", "slick" };
    }
    
    public sealed class RiverStone : Item
    {
        public override string ItemId => "torrent_source_river_stone";
        public override string DisplayName => "River Stone";
        public override string Description => "Smooth polished stone collectible from the source";
        public override List<string> OutcomeKeywords => new() { "stone", "river", "smooth", "polished", "rounded", "water-worn", "heavy", "dense", "grey", "collectible" };
    }
}
