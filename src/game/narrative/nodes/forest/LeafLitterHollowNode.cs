using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Leaf-Litter Hollow - A depression filled with decomposing leaves.
/// Associated with: Greenwood
/// </summary>
public class LeafLitterHollowNode : NarrationNode
{
    public override string NodeId => "leaf_litter_hollow";
    public override string ContextDescription => "exploring the leaf litter";
    public override string TransitionDescription => "descend into the hollow";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the soft dark <mulch> filling the hollow"), KeywordInContext.Parse("the hollow <depression> catching fallen leaves"), KeywordInContext.Parse("the smell of <decomposition> rising from the litter") };
    
    private static readonly string[] Moods = { "deep", "soft", "decomposing", "layered", "accumulated", "moist", "rich", "organic" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} leaf-litter hollow";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"exploring a {mood} leaf-litter hollow";
    }
    
    public override List<Item> GetItems() => new() { new LeafMold(), new Millipede() };

    public sealed class LeafMold : Item
    {
        public override string ItemId => "leaf_mold";
        public override string DisplayName => "Leaf Mold";
        public override string Description => "Rich, partially decomposed leaf matter";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some dark <humus> from decomposed leaves"), KeywordInContext.Parse("a faint <spore> cloud rising from the mold") };
    }
    
    public sealed class Millipede : Item
    {
        public override string ItemId => "leaf_litter_millipede";
        public override string DisplayName => "Millipede";
        public override string Description => "A long millipede coiling defensively";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a long <segment>ed millipede coiling up"), KeywordInContext.Parse("a slow <arthropod> moving through the litter") };
    }
}
