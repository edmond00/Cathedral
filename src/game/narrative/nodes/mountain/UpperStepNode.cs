using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class UpperStepNode : PyramidalFeatureNode
{
    public override int MinAltitude => 4;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(LowerStepNode);
    
    public override string NodeId => "upper_step";
    public override string ContextDescription => "on the upper stone step terrace";
    public override string TransitionDescription => "climb to the upper step";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a flat stone <terrace> cut into the upper slope"), KeywordInContext.Parse("a bold upward <step> in the mountain rock"), KeywordInContext.Parse("a raised stone <platform> above the lower tier"), KeywordInContext.Parse("a distinct <tier> in the layered mountainside") };
    
    private static readonly string[] Moods = { "elevated", "tiered", "layered", "stepped" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} upper step";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} upper step";
    }
    
}
