using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class WindScouredRidgeFlankedNode : PyramidalFeatureNode
{
    public override int MinAltitude => 0;
    public override int MaxAltitude => 2;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(WindScouredRidgeCrestNode);
    
    public override string NodeId => "wind_scoured_ridge_flanked";
    public override string ContextDescription => "standing on the scoured flank";
    public override string TransitionDescription => "move to the scoured flank";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "ice", "wind", "flank", "barrenness" };
    
    private static readonly string[] Moods = { "windswept", "harsh", "scoured", "desolate" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} scoured flank";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} scoured flank";
    }
    
}
