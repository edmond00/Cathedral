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
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a wide spread of wet <slush> across the slope"), KeywordInContext.Parse("a packed <snow> drifting across the spread"), KeywordInContext.Parse("the active <melting> spreading the channel"), KeywordInContext.Parse("the broad <fan> of slush below the channel") };
    
    private static readonly string[] Moods = { "dispersed", "soggy", "spreading", "melting" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} channel spread";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing in a {mood} slush channel spread";
    }
    
    public sealed class AlpineSedge : Item
    {
        public override string ItemId => "slush_channel_spread_alpine_sedge";
        public override string DisplayName => "Alpine Sedge";
        public override string Description => "Hardy grass collectible from the channel";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a stiff sedge <blade> poking through the slush"), KeywordInContext.Parse("a dense <tuft> of alpine sedge"), KeywordInContext.Parse("a frozen <tussock> at the channel edge") };
    }
}
