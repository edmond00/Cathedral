using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class HardSnowSlopeLowerNode : PyramidalFeatureNode
{
    public override int MinAltitude => 2;
    public override int MaxAltitude => 6;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(HardSnowSlopeUpperNode);
    
    public override string NodeId => "lower_hard_snow_slope";
    public override string ContextDescription => "standing on the lower hard snow slope";
    public override string TransitionDescription => "descend to the lower hard snow slope";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the broad <snowfield> gleaming below the peak"), KeywordInContext.Parse("the firm <slope> of compacted hard snow"), KeywordInContext.Parse("the wide <expanse> of unbroken white"), KeywordInContext.Parse("the crunch of the snow <hardness> underfoot") };
    
    private static readonly string[] Moods = { "expansive", "bright", "wind-scoured", "firm" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} lower snowfield";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} lower snowfield";
    }
    
}
