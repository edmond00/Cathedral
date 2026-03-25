using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class FrozenStreamChannelNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 9;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(FrozenStreamSourceNode);
    
    public override string NodeId => "frozen_stream_channel";
    public override string ContextDescription => "standing in the frozen stream channel";
    public override string TransitionDescription => "follow the frozen stream channel";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "stream", "ice", "channel", "ribbon" };
    
    private static readonly string[] Moods = { "winding", "frozen", "sinuous", "icy" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} ice channel";
    }
    
}
