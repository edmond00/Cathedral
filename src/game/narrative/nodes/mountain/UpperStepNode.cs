using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class UpperStepNode : PyramidalFeatureNode
{
    public override int MinAltitude => 4;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(LowerStepNode);
    
    public override string NodeId => "upper_step";
    public override string ContextDescription => "on the upper stone step terrace";
    public override string TransitionDescription => "climb to the upper step";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "terrace", "step", "upper", "ledge", "platform", "flat", "elevated", "layered", "bench", "tier" };
    
    private static readonly string[] Moods = { "elevated", "tiered", "layered", "stepped" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} upper step";
    }
    
    public sealed class FlatTerrace : Item
    {
        public override string ItemId => "upper_step_flat_terrace";
        public override string DisplayName => "Flat Terrace";
        public override string Description => "Level stone platform";
        public override List<string> OutcomeKeywords => new() { "flat", "terrace", "level", "platform", "horizontal", "smooth", "stable", "walking", "easy", "wide" };
    }
    
    public sealed class ErosionLayer : Item
    {
        public override string ItemId => "upper_step_erosion_layer";
        public override string DisplayName => "Erosion Layer";
        public override string Description => "Visible strata in the rock";
        public override List<string> OutcomeKeywords => new() { "erosion", "layer", "strata", "lines", "bands", "geological", "ancient", "visible", "history", "sedimentary" };
    }
    
    public sealed class TerraceEdge : Item
    {
        public override string ItemId => "upper_step_terrace_edge";
        public override string DisplayName => "Terrace Edge";
        public override string Description => "Sharp drop to the next level";
        public override List<string> OutcomeKeywords => new() { "edge", "terrace", "drop", "sharp", "boundary", "step", "fall", "line", "rim", "border" };
    }
}
