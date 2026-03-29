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
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a smooth rounded <stone> in the current"), KeywordInContext.Parse("the cold <current> threading between rocks"), KeywordInContext.Parse("the carved <channel> of the mountain river"), KeywordInContext.Parse("the steady <flow> of glacial meltwater") };
    
    private static readonly string[] Moods = { "flowing", "rocky", "shallow", "active" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} riverbed";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"in a {mood} riverbed";
    }
    
    public override List<Item> GetItems() => new() { new RiverStones(), new RiverSand() };

    public sealed class RiverStones : Item
    {
        public override string ItemId => "riverbed_river_stones";
        public override string DisplayName => "River Stones";
        public override string Description => "Smooth rounded rocks in the water";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a smooth river <cobble> worn round"), KeywordInContext.Parse("the <fluvial> smoothing of water on rock"), KeywordInContext.Parse("the high <polish> of a water-rounded stone") };
    }
    
    public sealed class RiverSand : Item
    {
        public override string ItemId => "riverbed_river_sand";
        public override string DisplayName => "River Sand";
        public override string Description => "Coarse sand deposit collectible from the riverbed";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a dark bed of <alluvium> in the riverbed"), KeywordInContext.Parse("a pale <quartz> grain in the river sand"), KeywordInContext.Parse("a dark <mineral> speck catching the light") };
    }
}
