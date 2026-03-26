using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class WallTopNode : PyramidalFeatureNode
{
    public override int MinAltitude => 6;
    public override int MaxAltitude => 9;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(WallBaseNode);
    
    public override string NodeId => "wall_top";
    public override string ContextDescription => "at the lower cliff wall top";
    public override string TransitionDescription => "climb to the wall top";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "wall", "precipice", "overlook", "drop" };
    
    private static readonly string[] Moods = { "exposed", "vertical", "precipitous", "commanding" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} wall top";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"at a {mood} wall top";
    }
    
}
