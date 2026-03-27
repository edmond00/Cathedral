using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class SnowLadenValleyUpperNode : PyramidalFeatureNode
{
    public override int MinAltitude => 8;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(SnowLadenValleyLowerNode);
    
    public override string NodeId => "upper_snow_laden_valley";
    public override string ContextDescription => "standing in the upper snow-laden valley";
    public override string TransitionDescription => "reach the upper snow-laden valley";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the high snow-laden <valley> near the summit"), KeywordInContext.Parse("a packed <snow> drifting across the slope"), KeywordInContext.Parse("the vast white <expanse> of the upper valley"), KeywordInContext.Parse("the untouched <purity> of pristine deep snow") };
    
    private static readonly string[] Moods = { "pristine", "deep", "untouched", "expansive" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} upper valley";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing in a {mood} upper snow-laden valley";
    }
    
}
