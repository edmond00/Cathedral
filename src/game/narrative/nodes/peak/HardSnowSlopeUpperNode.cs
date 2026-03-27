using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class HardSnowSlopeUpperNode : PyramidalFeatureNode
{
    public override int MinAltitude => 2;
    public override int MaxAltitude => 6;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(HardSnowSlopeLowerNode);
    
    public override string NodeId => "upper_hard_snow_slope";
    public override string ContextDescription => "standing on the upper hard snow slope";
    public override string TransitionDescription => "climb to the upper hard snow slope";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the steep upper <snowfield> near the summit"), KeywordInContext.Parse("the hard-packed <slope> of wind-scoured snow"), KeywordInContext.Parse("the cutting <wind> across the upper snowfield"), KeywordInContext.Parse("the ice-like <hardness> of the compacted snow") };
    
    private static readonly string[] Moods = { "hard-packed", "gleaming", "steep", "wind-hardened" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} upper snowfield";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} upper snowfield";
    }
    
}
