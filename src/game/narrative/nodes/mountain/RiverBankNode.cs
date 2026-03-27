using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class RiverBankNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 9;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(RiverbedNode);
    
    public override string NodeId => "river_bank";
    public override string ContextDescription => "on the river cut bank";
    public override string TransitionDescription => "climb to the river bank";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a steep cut <bank> above the water"), KeywordInContext.Parse("the cold <river> rushing below"), KeywordInContext.Parse("a crumbling <embankment> undercut by current"), KeywordInContext.Parse("the raw face of recent <erosion>") };
    
    private static readonly string[] Moods = { "eroded", "undercut", "steep", "crumbling" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} river bank";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} river bank";
    }
    
    public sealed class RootsExposed : Item
    {
        public override string ItemId => "river_bank_roots_exposed";
        public override string DisplayName => "Exposed Roots";
        public override string Description => "Tree roots hanging from eroded bank";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a pale <tendril> of root hanging from the bank"), KeywordInContext.Parse("the continuing <erosion> of the river bank"), KeywordInContext.Parse("an intricate <network> of roots in the soil") };
    }
    
    public sealed class RiverGrass : Item
    {
        public override string ItemId => "river_bank_river_grass";
        public override string DisplayName => "River Grass";
        public override string Description => "Tall grass growing at water's edge";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a flat grass <blade> bending over the water"), KeywordInContext.Parse("a tall <reed> growing at the bank edge"), KeywordInContext.Parse("the rich <waterside> vegetation of the bank") };
    }
}
