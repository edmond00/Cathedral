using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class UpperScreeNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(LowerScreeNode);
    
    public override string NodeId => "upper_scree";
    public override string ContextDescription => "on the upper scree slope";
    public override string TransitionDescription => "climb to the upper scree";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("some loose <scree> shifting with every step"), KeywordInContext.Parse("a sharp angular <rock> among the debris"), KeywordInContext.Parse("the constant <instability> of the upper slope"), KeywordInContext.Parse("the genuine <danger> of the sliding scree") };
    
    private static readonly string[] Moods = { "treacherous", "unstable", "sliding", "precarious" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} upper scree";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} upper scree";
    }
    
    public override List<Item> GetItems() => new() { new LooseChips() };

    public sealed class LooseChips : Item
    {
        public override string ItemId => "upper_scree_loose_chips";
        public override string DisplayName => "Loose Chips";
        public override string Description => "Small angular rock fragments";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a small angular <fragment> of fresh stone"), KeywordInContext.Parse("a pinch of coarse <gravel> underfoot"), KeywordInContext.Parse("a thin <shard> of split rock") };
    }
    
}
