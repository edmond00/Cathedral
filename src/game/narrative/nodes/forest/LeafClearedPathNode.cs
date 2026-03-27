using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Leaf-Cleared Path - A transversal feature maintained by wind or passage.
/// </summary>
public class LeafClearedPathNode : NarrationNode
{
    public override string NodeId => "leaf_cleared_path";
    public override string ContextDescription => "walking the leaf-cleared path";
    public override string TransitionDescription => "take the cleared path";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a clear <path> swept free of leaves"), KeywordInContext.Parse("the <wind> that must have cleared the way"), KeywordInContext.Parse("a surprising <clarity> in an otherwise cluttered forest") };
    
    private static readonly string[] Moods = { "swept", "clear", "tidy", "clean", "maintained", "neat", "orderly", "pristine" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} leaf-cleared path";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"walking a {mood} leaf-cleared path";
    }
    
    public sealed class SweptLeaves : Item
    {
        public override string ItemId => "leaf_cleared_path_swept_leaves";
        public override string DisplayName => "Swept Leaves";
        public override string Description => "Leaves gathered from the edges of the cleared path";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a crisp dry <lamina> from the swept edges"), KeywordInContext.Parse("a <pile> of leaves blown to the path's margin") };
    }
}
