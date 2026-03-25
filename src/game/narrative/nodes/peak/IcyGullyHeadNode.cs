using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class IcyGullyHeadNode : PyramidalFeatureNode
{
    public override int MinAltitude => 4;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(IcyGullyRunNode);
    
    public override string NodeId => "icy_gully_head";
    public override string ContextDescription => "standing at the icy gully head";
    public override string TransitionDescription => "reach the icy gully head";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "gully", "ice", "chute", "source" };
    
    private static readonly string[] Moods = { "steep", "narrow", "icy", "channeled" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} gully head";
    }
    
}
