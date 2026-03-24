using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class FrozenStreamSourceNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 9;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(FrozenStreamChannelNode);
    
    public override string NodeId => "frozen_stream_source";
    public override string ContextDescription => "standing at the frozen stream source";
    public override string TransitionDescription => "reach the frozen stream source";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "stream", "source", "frozen", "spring", "ice", "origin", "cold", "seep", "nascent", "beginning" };
    
    private static readonly string[] Moods = { "nascent", "frozen", "pristine", "originating" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} frozen source";
    }
    
    public sealed class SpringQuartz : Item
    {
        public override string ItemId => "frozen_stream_source_spring_quartz";
        public override string DisplayName => "Spring Quartz";
        public override string Description => "Clear quartz crystal collectible from the spring";
        public override List<string> OutcomeKeywords => new() { "quartz", "crystal", "clear", "spring", "transparent", "hexagonal", "hard", "mineral", "pure", "collectible" };
    }
}
