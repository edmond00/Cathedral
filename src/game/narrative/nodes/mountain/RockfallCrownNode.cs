using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class RockfallCrownNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(DebrisFieldNode);
    
    public override string NodeId => "rockfall_crown";
    public override string ContextDescription => "at the rockfall crown";
    public override string TransitionDescription => "climb to the rockfall crown";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "rockfall", "fracture", "scar", "collapse" };
    
    private static readonly string[] Moods = { "fractured", "collapsed", "scarred", "broken" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} rockfall crown";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"at a {mood} rockfall crown";
    }
    
}
