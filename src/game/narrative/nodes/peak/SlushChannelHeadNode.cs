using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class SlushChannelHeadNode : PyramidalFeatureNode
{
    public override int MinAltitude => 7;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(SlushChannelSpreadNode);
    
    public override string NodeId => "slush_channel_head";
    public override string ContextDescription => "standing at the slush channel head";
    public override string TransitionDescription => "reach the slush channel head";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "slush", "channel", "melting", "transition" };
    
    private static readonly string[] Moods = { "slushy", "melting", "wet", "transitional" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} channel head";
    }
    
}
