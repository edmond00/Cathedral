using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class GlacierTongueUpperNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(GlacierTongueLowerNode);
    
    public override string NodeId => "upper_glacier_tongue";
    public override string ContextDescription => "standing on the upper ice flow";
    public override string TransitionDescription => "climb to the upper ice flow";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "glacier", "ice", "flow", "antiquity" };
    
    private static readonly string[] Moods = { "massive", "flowing", "ancient", "frozen" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} upper ice flow";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} upper ice flow";
    }
    
}
