using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Woodpecker Tree - A tree actively worked by woodpeckers.
/// </summary>
public class WoodpeckerTreeNode : NarrationNode
{
    public override string NodeId => "woodpecker_tree";
    public override string ContextDescription => "examining the woodpecker tree";
    public override string TransitionDescription => "approach the pecked tree";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a neat round <hole> drilled into the trunk"), KeywordInContext.Parse("the echo of <drumming> still audible from the canopy"), KeywordInContext.Parse("the scattered <bark> chips below the workings"), KeywordInContext.Parse("a deep <cavity> carved into the wood by repeated blows") };
    
    private static readonly string[] Moods = { "rhythmic", "pecked", "excavated", "riddled", "hollowed", "worked", "tapped", "resonant" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} woodpecker tree";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} woodpecker tree";
    }
    
    public sealed class WoodChips : Item
    {
        public override string ItemId => "woodpecker_chips";
        public override string DisplayName => "Wood Chips";
        public override string Description => "Fresh wood chips from woodpecker excavation";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a small curved <fragment> of wood chip from below the hole"), KeywordInContext.Parse("a pale piece of exposed <heartwood> from deep in the trunk"), KeywordInContext.Parse("a fine <splinter> from the edge of the excavation") };
    }
    
    public sealed class BarkFragment : Item
    {
        public override string ItemId => "woodpecker_tree_bark_fragment";
        public override string DisplayName => "Pecked Bark Fragment";
        public override string Description => "Bark pieces loosened by woodpecker drilling";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a piece of outer <cortex> bark prised loose by the bird"), KeywordInContext.Parse("a fragment still curved from the <hole> in the trunk wall"), KeywordInContext.Parse("a flat brittle <shard> of bark from the drilling site") };
    }
}
