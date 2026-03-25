using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class WindPackedDriftHollowNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 6;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(WindPackedDriftCrestNode);
    
    public override string NodeId => "wind_packed_drift_hollow";
    public override string ContextDescription => "standing in the drift hollow";
    public override string TransitionDescription => "descend into the drift hollow";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "hollow", "drift", "snow", "shelter" };
    
    private static readonly string[] Moods = { "sheltered", "scooped", "concave", "protected" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} drift hollow";
    }
    
}
