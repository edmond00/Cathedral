using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class TorrentChannelNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 9;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(TorrentSourceNode);
    
    public override string NodeId => "torrent_channel";
    public override string ContextDescription => "in the mountain torrent channel";
    public override string TransitionDescription => "descend to the torrent channel";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the roaring <torrent> cutting through stone"), KeywordInContext.Parse("the narrow <channel> carved by the rushing water"), KeywordInContext.Parse("the deep <erosion> of the torrent walls"), KeywordInContext.Parse("the icy <water> churning at your feet") };
    
    private static readonly string[] Moods = { "rushing", "carved", "narrow", "powerful" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} torrent channel";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"in a {mood} torrent channel";
    }
    
    public sealed class TorrentGravel : Item
    {
        public override string ItemId => "torrent_channel_torrent_gravel";
        public override string DisplayName => "Torrent Gravel";
        public override string Description => "Water-rounded pebbles collectible from the channel";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a water-rounded <pebble> from the torrent"), KeywordInContext.Parse("the constant <tumbling> of stone in the current"), KeywordInContext.Parse("the deep <smoothness> of a river-polished rock") };
    }
    
}
