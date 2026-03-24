using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class FanApexNode : PyramidalFeatureNode
{
    public override int MinAltitude => 7;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(FanSpreadNode);
    
    public override string NodeId => "fan_apex";
    public override string ContextDescription => "at the alluvial fan apex";
    public override string TransitionDescription => "climb to the fan apex";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "fan", "apex", "top", "origin", "channel", "gravel", "stream", "spreading", "source", "upper" };
    
    private static readonly string[] Moods = { "spreading", "radiating", "distributive", "channeled" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} fan apex";
    }
    
    public sealed class ChannelSplit : Item
    {
        public override string ItemId => "fan_apex_channel_split";
        public override string DisplayName => "Channel Split";
        public override string Description => "Point where water divides into multiple paths";
        public override List<string> OutcomeKeywords => new() { "channel", "split", "divide", "branching", "distributary", "fork", "separation", "flow", "diverging", "multiple" };
    }
    
    public sealed class CoarseGravel : Item
    {
        public override string ItemId => "fan_apex_coarse_gravel";
        public override string DisplayName => "Coarse Gravel";
        public override string Description => "Large stones at the fan origin";
        public override List<string> OutcomeKeywords => new() { "coarse", "gravel", "large", "stones", "angular", "rough", "heavy", "deposit", "accumulated", "sorted" };
    }
    
    public sealed class FastFlow : Item
    {
        public override string ItemId => "fan_apex_fast_flow";
        public override string DisplayName => "Fast Flow";
        public override string Description => "Rapid water movement at the apex";
        public override List<string> OutcomeKeywords => new() { "fast", "flow", "rapid", "water", "swift", "rushing", "powerful", "current", "moving", "energetic" };
    }
}
