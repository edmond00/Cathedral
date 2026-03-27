using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class FanApexNode : PyramidalFeatureNode
{
    public override int MinAltitude => 7;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(FanSpreadNode);
    
    public override string NodeId => "fan_apex";
    public override string ContextDescription => "at the alluvial fan apex";
    public override string TransitionDescription => "climb to the fan apex";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the spreading alluvial <fan> above"), KeywordInContext.Parse("the narrow <apex> where channels begin"), KeywordInContext.Parse("a thick bed of coarse <gravel> underfoot"), KeywordInContext.Parse("the original <source> of the alluvial flow") };
    
    private static readonly string[] Moods = { "spreading", "radiating", "distributive", "channeled" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} fan apex";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"at a {mood} fan apex";
    }
    
    public sealed class CoarseGravel : Item
    {
        public override string ItemId => "fan_apex_coarse_gravel";
        public override string DisplayName => "Coarse Gravel";
        public override string Description => "Large stones at the fan origin";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a water-rounded <pebble> at the fan origin"), KeywordInContext.Parse("a flat <stone> sorted by the current"), KeywordInContext.Parse("a layered <deposit> of mixed sediment") };
    }
    
}
