using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class ArchCrestNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(ArchPassageNode);
    
    public override string NodeId => "arch_crest";
    public override string ContextDescription => "atop the rock arch crest";
    public override string TransitionDescription => "climb to the arch crest";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a curved rock <arch> overhead"), KeywordInContext.Parse("the sharp <crest> of the arch above"), KeywordInContext.Parse("a dramatic <formation> of wind-carved stone"), KeywordInContext.Parse("a graceful <span> bridging the gap") };
    
    private static readonly string[] Moods = { "graceful", "curved", "spectacular", "precarious" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} arch crest";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"atop a {mood} arch crest";
    }
    
    public sealed class ArchKeystone : Item
    {
        public override string ItemId => "arch_crest_keystone";
        public override string DisplayName => "Arch Keystone";
        public override string Description => "Central supporting stone at arch peak";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the central <capstone> locking the arch in place"), KeywordInContext.Parse("the highest <apex> of the stone span"), KeywordInContext.Parse("the whole <structure> balanced against the sky") };
    }
    
}
