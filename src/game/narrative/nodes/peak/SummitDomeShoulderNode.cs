using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class SummitDomeShoulderNode : PyramidalFeatureNode
{
    public override int MinAltitude => 0;
    public override int MaxAltitude => 1;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(SummitDomeCrestNode);
    
    public override string NodeId => "summit_dome_shoulder";
    public override string ContextDescription => "standing on the summit dome shoulder";
    public override string TransitionDescription => "move to the summit dome shoulder";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "shoulder", "ice", "slope", "summit" };
    
    private static readonly string[] Moods = { "curved", "exposed", "steep", "frozen" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} summit dome shoulder";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} summit dome shoulder";
    }
    
}
