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
    
    public override List<string> NodeKeywords => new() { "torrent", "channel", "erosion", "water" };
    
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
        public override List<string> OutcomeKeywords => new() { "pebble", "tumbling", "smoothness" };
    }
    
}
