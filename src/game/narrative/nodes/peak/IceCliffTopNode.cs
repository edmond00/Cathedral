using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class IceCliffTopNode : PyramidalFeatureNode
{
    public override int MinAltitude => 2;
    public override int MaxAltitude => 5;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(IceCliffBaseNode);
    
    public override string NodeId => "ice_cliff_top";
    public override string ContextDescription => "standing at the ice cliff top";
    public override string TransitionDescription => "reach the ice cliff top";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "cliff", "top", "ice", "edge", "precipice", "frozen", "vertical", "drop", "blue", "massive" };
    
    private static readonly string[] Moods = { "imposing", "frozen", "precipitous", "towering" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} ice cliff top";
    }
    
}
