using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Mixed Underwood - Level 5. Dense saplings and bramble undergrowth.
/// </summary>
public class MixedUnderwoodNode : NarrationNode
{
    public override string NodeId => "mixed_underwood";
    public override string ContextDescription => "pushing through mixed underwood";
    public override string TransitionDescription => "enter the underwood";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a sprawling <bramble> cane across the path"), KeywordInContext.Parse("a crowded young <sapling> among older stems"), KeywordInContext.Parse("the dense <undergrowth> pressing in on both sides"), KeywordInContext.Parse("a sharp <thorn> drawing blood from the wrist") };
    
    private static readonly string[] Moods = { "tangled", "crowded", "wild", "unruly", "chaotic", "cluttered", "overgrown", "untamed" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} mixed underwood";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"pushing through a {mood} mixed underwood";
    }
    
    public sealed class TangledVines : Item
    {
        public override string ItemId => "mixed_underwood_tangled_vines";
        public override string DisplayName => "Tangled Vines";
        public override string Description => "Twisted vines from the dense underwood";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a twisted <liana> vine roped around a stem"), KeywordInContext.Parse("a hopeless <tangle> of mixed underwood growth") };
    }
    
    public sealed class UnderbrushStems : Item
    {
        public override string ItemId => "mixed_underwood_underbrush_stems";
        public override string DisplayName => "Underbrush Stems";
        public override string Description => "Flexible stems from varied undergrowth";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a hollow pale <internode> between two leaf joints"), KeywordInContext.Parse("a fresh <shoot> pushing past the older stems") };
    }
}
