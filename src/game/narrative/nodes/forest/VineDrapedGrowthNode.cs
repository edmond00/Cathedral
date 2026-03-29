using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Vine-Draped Growth - Shrubs and saplings covered in climbing vines.
/// Associated with: DenseThicketland
/// </summary>
public class VineDrapedGrowthNode : NarrationNode
{
    public override string NodeId => "vine_draped_growth";
    public override string ContextDescription => "pushing through vine-draped growth";
    public override string TransitionDescription => "enter the vine tangle";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a thick green <vine> looped between the branches"), KeywordInContext.Parse("a curling <tendril> reaching for any hold it can find"), KeywordInContext.Parse("the steady <climbing> progress of stems up every surface"), KeywordInContext.Parse("an <entanglement> of shoots that pulls at every step") };
    
    private static readonly string[] Moods = { "draped", "vine-covered", "entangled", "hanging", "wrapped", "smothered", "festooned", "shrouded" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} vine-draped growth";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"pushing through a {mood} vine-draped growth";
    }
    
    public override List<Item> GetItems() => new() { new VineTendril(), new VineLeaf() };

    public sealed class VineTendril : Item
    {
        public override string ItemId => "vine_tendril";
        public override string DisplayName => "Vine Tendril";
        public override string Description => "A flexible climbing tendril";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a delicate coiled <cirrus> still reaching outward"), KeywordInContext.Parse("a length of woody <liana> stripped from the shrub"), KeywordInContext.Parse("the tight spring of its <curl> still in the stem") };
    }
    
    public sealed class VineLeaf : Item
    {
        public override string ItemId => "vine_draped_leaf";
        public override string DisplayName => "Vine Leaf";
        public override string Description => "A broad heart-shaped leaf from the climbing vines";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the wide flat <lamina> of a heart-shaped vine leaf"), KeywordInContext.Parse("a broad leaf torn from the climbing <liana>"), KeywordInContext.Parse("the rounded <lobe> at the base of the leaf blade"), KeywordInContext.Parse("a raised pale <vein> running through the leaf") };
    }
}
