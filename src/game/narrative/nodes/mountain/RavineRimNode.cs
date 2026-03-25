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
    
    public sealed class OverhangingEdge : Item
    {
        public override string ItemId => "ravine_rim_overhanging_edge";
        public override string DisplayName => "Overhanging Edge";
        public override string Description => "Rock jutting over the ravine";
        public override List<string> OutcomeKeywords => new() { "overhang", "edge", "danger" };
    }
    
    public sealed class EchoSound : Item
    {
        public override string ItemId => "ravine_rim_echo_sound";
        public override string DisplayName => "Echo Sound";
        public override string Description => "Sounds reverberating in the chasm";
        public override List<string> OutcomeKeywords => new() { "echo", "resonance", "depth" };
    }
    
    public sealed class CliffSwallow : Item
    {
        public override string ItemId => "ravine_rim_cliff_swallow";
        public override string DisplayName => "Cliff Swallow";
        public override string Description => "Bird darting through the ravine";
        public override List<string> OutcomeKeywords => new() { "swallow", "bird", "agility" };
    }
}
