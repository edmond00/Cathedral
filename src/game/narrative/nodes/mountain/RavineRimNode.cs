using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class RavineRimNode : PyramidalFeatureNode
{
    public override int MinAltitude => 4;
    public override int MaxAltitude => 8;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(RavineFloorNode);
    
    public override string NodeId => "ravine_rim";
    public override string ContextDescription => "at the narrow ravine rim";
    public override string TransitionDescription => "approach the ravine rim";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "rim", "chasm", "gorge", "precipice" };
    
    private static readonly string[] Moods = { "narrow", "vertiginous", "precipitous", "dizzying" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} ravine rim";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"at a {mood} ravine rim";
    }
    
}
