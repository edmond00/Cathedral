using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class GullyLipNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 8;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(GullyBottomNode);
    
    public override string NodeId => "gully_lip";
    public override string ContextDescription => "at the shaded gully lip";
    public override string TransitionDescription => "approach the gully lip";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "lip", "rim", "edge", "drop" };
    
    private static readonly string[] Moods = { "shaded", "dark", "shadowy", "dim" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} gully lip";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"at a {mood} gully lip";
    }
    
    public sealed class OverhangingFern : Item
    {
        public override string ItemId => "gully_lip_overhanging_fern";
        public override string DisplayName => "Overhanging Fern";
        public override string Description => "Lush fern growing at the gully edge";
        public override List<string> OutcomeKeywords => new() { "fern", "frond", "overhang" };
    }
    
    public sealed class CoolAir : Item
    {
        public override string ItemId => "gully_lip_cool_air";
        public override string DisplayName => "Cool Air";
        public override string Description => "Cold air rising from the gully";
        public override List<string> OutcomeKeywords => new() { "draft", "chill", "breeze" };
    }
    
    public sealed class SlipperyEdge : Item
    {
        public override string ItemId => "gully_lip_slippery_edge";
        public override string DisplayName => "Slippery Edge";
        public override string Description => "Moist rock at the gully entrance";
        public override List<string> OutcomeKeywords => new() { "edge", "slipperiness", "moss" };
    }
}
