using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class FanSpreadNode : PyramidalFeatureNode
{
    public override int MinAltitude => 7;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(FanApexNode);
    
    public override string NodeId => "fan_spread";
    public override string ContextDescription => "on the alluvial fan spread";
    public override string TransitionDescription => "descend to the fan spread";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the wide alluvial <fan> spread below"), KeywordInContext.Parse("a thin sheet of <gravel> across the surface"), KeywordInContext.Parse("a fresh <deposit> of sorted material"), KeywordInContext.Parse("a braided <distributary> crossing the fan") };
    
    private static readonly string[] Moods = { "wide", "gentle", "spreading", "deposited" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} fan spread";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} fan spread";
    }
    
    public override List<Item> GetItems() => new() { new FanGravel() };

    public sealed class FanGravel : Item
    {
        public override string ItemId => "fan_gravel";
        public override string DisplayName => "Fine Gravel";
        public override string Description => "Small sorted stones across the fan";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a small <fragment> of transported stone"), KeywordInContext.Parse("a thin <deposit> of fine-grained material"), KeywordInContext.Parse("a visible <layer> of sorted sediment") };
    }
    
}
