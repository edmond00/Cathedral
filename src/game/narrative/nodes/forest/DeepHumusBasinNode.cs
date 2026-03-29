using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Deep Humus Basin - A depression filled with rich, decomposed organic matter.
/// Associated with: Oldgrowth
/// </summary>
public class DeepHumusBasinNode : NarrationNode
{
    public override string NodeId => "deep_humus_basin";
    public override string ContextDescription => "exploring the humus basin";
    public override string TransitionDescription => "descend into the basin";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("some crumbly black <humus> filling the hollow"), KeywordInContext.Parse("the deep <basin> shaped by centuries of accumulation"), KeywordInContext.Parse("an extraordinary <fertility> in the rich dark earth") };
    
    private static readonly string[] Moods = { "deep", "rich", "black", "fertile", "accumulated", "organic", "soft", "ancient" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} deep humus basin";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"exploring a {mood} deep humus basin";
    }
    
    public override List<Item> GetItems() => new() { new PureHumus(), new AncientSeed() };

    public sealed class PureHumus : Item
    {
        public override string ItemId => "pure_humus";
        public override string DisplayName => "Pure Humus";
        public override string Description => "Black, crumbly humus from centuries of decay";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the crumbly dark <loam> lifting easily"), KeywordInContext.Parse("a rich <soil> built from centuries of decay"), KeywordInContext.Parse("an intense <decomposition> smell rising from below") };
    }
    
    public sealed class AncientSeed : Item
    {
        public override string ItemId => "deep_humus_ancient_seed";
        public override string DisplayName => "Ancient Seed";
        public override string Description => "A long-buried seed preserved in the humus";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a hard <kernel> preserved in the deep humus"), KeywordInContext.Parse("a cracked <husk> around an ancient seed") };
    }
}
