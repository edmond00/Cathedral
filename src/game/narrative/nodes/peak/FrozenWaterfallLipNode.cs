using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class FrozenWaterfallLipNode : PyramidalFeatureNode
{
    public override int MinAltitude => 4;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(FrozenWaterfallBaseNode);
    
    public override string NodeId => "frozen_waterfall_lip";
    public override string ContextDescription => "standing at the frozen waterfall lip";
    public override string TransitionDescription => "approach the frozen waterfall lip";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "waterfall", "ice", "lip", "suspension" };
    
    private static readonly string[] Moods = { "suspended", "frozen", "cascading", "precipitous" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} waterfall lip";
    }
    
}
