using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class FrozenRidgeFaceLowerNode : PyramidalFeatureNode
{
    public override int MinAltitude => 1;
    public override int MaxAltitude => 3;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(FrozenRidgeFaceUpperNode);
    
    public override string NodeId => "lower_frozen_ridge_face";
    public override string ContextDescription => "standing at the lower frozen ridge face";
    public override string TransitionDescription => "descend to the lower frozen ridge face";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "ice", "face", "wall", "verticality" };
    
    private static readonly string[] Moods = { "towering", "frozen", "imposing", "icy" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} lower frozen ridge face";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing at a {mood} lower frozen ridge face";
    }
    
}
