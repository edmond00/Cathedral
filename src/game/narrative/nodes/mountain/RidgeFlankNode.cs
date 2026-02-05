using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class RidgeFlankNode : PyramidalFeatureNode
{
    public override int MinAltitude => 0;
    public override int MaxAltitude => 3;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(RidgeSpineNode);
    
    public override string NodeId => "ridge_flank";
    public override string ContextDescription => "on the exposed ridge flank";
    public override string TransitionDescription => "descend to the ridge flank";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "flank", "side", "slope", "angled", "steep", "exposed", "grassy", "rocky", "descending", "inclined" };
    
    private static readonly string[] Moods = { "sloping", "angled", "steep", "descending" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} ridge flank";
    }
    
    public sealed class AlpineGrass : Item
    {
        public override string ItemId => "ridge_flank_alpine_grass";
        public override string DisplayName => "Alpine Grass";
        public override string Description => "Hardy grass clinging to the ridge side";
        public override List<string> OutcomeKeywords => new() { "alpine", "grass", "hardy", "clinging", "tough", "sparse", "windswept", "green", "clumped", "stubborn" };
    }
    
    public sealed class SlopeDebris : Item
    {
        public override string ItemId => "ridge_flank_slope_debris";
        public override string DisplayName => "Slope Debris";
        public override string Description => "Loose rock and gravel on the flank";
        public override List<string> OutcomeKeywords => new() { "debris", "loose", "gravel", "scattered", "unstable", "rocky", "fragments", "sliding", "shifting", "mobile" };
    }
}
