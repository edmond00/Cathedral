using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class RiverbedNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 9;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(RiverBankNode);
    
    public override string NodeId => "riverbed";
    public override string ContextDescription => "in the river cut bed";
    public override string TransitionDescription => "descend to the riverbed";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "riverbed", "water", "stones", "flow", "shallow", "current", "rocks", "channel", "wet", "stream" };
    
    private static readonly string[] Moods = { "flowing", "rocky", "shallow", "active" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} riverbed";
    }
    
    public sealed class RiverStones : Item
    {
        public override string ItemId => "riverbed_river_stones";
        public override string DisplayName => "River Stones";
        public override string Description => "Smooth rounded rocks in the water";
        public override List<string> OutcomeKeywords => new() { "stones", "river", "smooth", "rounded", "water", "polished", "wet", "colorful", "varied", "tumbled" };
    }
    
    public sealed class RiverSand : Item
    {
        public override string ItemId => "riverbed_river_sand";
        public override string DisplayName => "River Sand";
        public override string Description => "Coarse sand deposit collectible from the riverbed";
        public override List<string> OutcomeKeywords => new() { "sand", "coarse", "granular", "gritty", "quartz", "golden", "mineral", "sediment", "loose", "collectible" };
    }
}
