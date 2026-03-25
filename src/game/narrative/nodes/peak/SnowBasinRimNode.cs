using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class SnowBasinRimNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 8;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(SnowBasinFloorNode);
    
    public override string NodeId => "snow_basin_rim";
    public override string ContextDescription => "standing on the snow basin rim";
    public override string TransitionDescription => "reach the snow basin rim";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "basin", "rim", "crater", "boundary" };
    
    private static readonly string[] Moods = { "circular", "enclosing", "elevated", "sheltering" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} basin rim";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} snow basin rim";
    }
    
}
