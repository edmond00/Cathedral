using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class SnowLadenValleyUpperNode : PyramidalFeatureNode
{
    public override int MinAltitude => 8;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(SnowLadenValleyLowerNode);
    
    public override string NodeId => "snow_laden_valley_upper";
    public override string ContextDescription => "standing in the upper snow-laden valley";
    public override string TransitionDescription => "reach the upper snow-laden valley";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "valley", "snow", "expanse", "pristine" };
    
    private static readonly string[] Moods = { "pristine", "deep", "untouched", "expansive" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} upper valley";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing in a {mood} upper snow-laden valley";
    }
    
}
