using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class ChannelBankNode : PyramidalFeatureNode
{
    public override int MinAltitude => 8;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(ChannelBedNode);
    
    public override string NodeId => "channel_bank";
    public override string ContextDescription => "on the floodplain channel bank";
    public override string TransitionDescription => "climb to the channel bank";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "bank", "channel", "levee", "floodplain" };
    
    private static readonly string[] Moods = { "gentle", "grassy", "raised", "vegetated" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} channel bank";
    }
    
    public sealed class ReedBed : Item
    {
        public override string ItemId => "channel_bank_reed_bed";
        public override string DisplayName => "Reed Bed";
        public override string Description => "Tall reeds growing at water's edge";
        public override List<string> OutcomeKeywords => new() { "reed", "bed", "waterside" };
    }
    
    public sealed class BankSediment : Item
    {
        public override string ItemId => "channel_bank_sediment";
        public override string DisplayName => "Bank Sediment";
        public override string Description => "Layered deposits from past floods";
        public override List<string> OutcomeKeywords => new() { "sediment", "silt", "deposit" };
    }
    
    public sealed class MountainReedStem : Item
    {
        public override string ItemId => "mountain_reed_stem";
        public override string DisplayName => "Mountain Reed Stem";
        public override string Description => "Dried reed stem collectible from the bank";
        public override List<string> OutcomeKeywords => new() { "stem", "reed", "hollow" };
    }
}
