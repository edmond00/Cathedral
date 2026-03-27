using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class FoothillUpperRiseNode : PyramidalFeatureNode
{
    public override int MinAltitude => 9;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(FoothillLowerSlopeNode);
    
    public override string NodeId => "foothill_upper_rise";
    public override string ContextDescription => "on the foothill upper rise";
    public override string TransitionDescription => "climb to the foothill upper rise";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the rolling <foothill> climbing toward rock"), KeywordInContext.Parse("a steady <rise> underfoot as the mountain begins"), KeywordInContext.Parse("a long <slope> of thin grass and stone"), KeywordInContext.Parse("the last <grass> before the bare rock above") };
    
    private static readonly string[] Moods = { "rolling", "gentle", "grassy", "elevated" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} foothill upper rise";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} foothill upper rise";
    }
    
}
