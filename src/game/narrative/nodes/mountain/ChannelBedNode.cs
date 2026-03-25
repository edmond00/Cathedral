using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class ChannelBedNode : PyramidalFeatureNode
{
    public override int MinAltitude => 8;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(ChannelBankNode);
    
    public override string NodeId => "channel_bed";
    public override string ContextDescription => "in the floodplain channel bed";
    public override string TransitionDescription => "descend to the channel bed";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "channel", "bed", "floodplain", "sand" };
    
    private static readonly string[] Moods = { "shallow", "gentle", "flowing", "sandy" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} channel bed";
    }
    
    public sealed class SandBar : Item
    {
        public override string ItemId => "channel_bed_sand_bar";
        public override string DisplayName => "Sand Bar";
        public override string Description => "Exposed sand deposit in the channel";
        public override List<string> OutcomeKeywords => new() { "sandbar", "deposit", "island" };
    }
    
    public sealed class SiltStone : Item
    {
        public override string ItemId => "channel_bed_silt_stone";
        public override string DisplayName => "Silt Stone";
        public override string Description => "Fine sedimentary rock collectible from the channel bed";
        public override List<string> OutcomeKeywords => new() { "siltstone", "sediment", "mineral" };
    }
}
