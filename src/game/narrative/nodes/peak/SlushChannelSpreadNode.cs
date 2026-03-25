using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class SlushChannelSpreadNode : PyramidalFeatureNode
{
    public override int MinAltitude => 7;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(SlushChannelHeadNode);
    
    public override string NodeId => "slush_channel_spread";
    public override string ContextDescription => "standing in the slush channel spread";
    public override string TransitionDescription => "descend to the slush channel spread";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "slush", "snow", "melting", "spread" };
    
    private static readonly string[] Moods = { "dispersed", "soggy", "spreading", "melting" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} channel spread";
    }
    
    public sealed class SlushPool : Item
    {
        public override string ItemId => "slush_channel_spread_slush_pool";
        public override string DisplayName => "Slush Pool";
        public override string Description => "Pool of accumulated slush";
        public override List<string> OutcomeKeywords => new() { "slush", "pool", "water" };
    }
    
    public sealed class AlpineSedge : Item
    {
        public override string ItemId => "slush_channel_spread_alpine_sedge";
        public override string DisplayName => "Alpine Sedge";
        public override string Description => "Hardy grass collectible from the channel";
        public override List<string> OutcomeKeywords => new() { "sedge", "alpine", "grass" };
    }
}
