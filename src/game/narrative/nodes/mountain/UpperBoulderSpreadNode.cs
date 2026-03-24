using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class UpperBoulderSpreadNode : PyramidalFeatureNode
{
    public override int MinAltitude => 4;
    public override int MaxAltitude => 8;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(LowerBoulderSpreadNode);
    
    public override string NodeId => "upper_boulder_spread";
    public override string ContextDescription => "in the upper boulder spread";
    public override string TransitionDescription => "climb into the upper boulder spread";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "boulders", "spread", "massive", "scattered", "rocky", "field", "obstacles", "maze", "jumbled", "glacial" };
    
    private static readonly string[] Moods = { "massive", "scattered", "chaotic", "maze-like" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} upper boulder spread";
    }
    
    public sealed class RoundedBoulder : Item
    {
        public override string ItemId => "upper_boulder_spread_rounded_boulder";
        public override string DisplayName => "Rounded Boulder";
        public override string Description => "Large water-smoothed stone";
        public override List<string> OutcomeKeywords => new() { "rounded", "boulder", "smooth", "large", "glacial", "ancient", "weathered", "gray", "massive", "solid" };
    }
    
    public sealed class BoulderGap : Item
    {
        public override string ItemId => "upper_boulder_spread_boulder_gap";
        public override string DisplayName => "Boulder Gap";
        public override string Description => "Space between massive rocks";
        public override List<string> OutcomeKeywords => new() { "gap", "space", "between", "passage", "opening", "narrow", "shadowed", "squeeze", "crevice", "slot" };
    }
    
    public sealed class BoulderLichen : Item
    {
        public override string ItemId => "boulder_lichen";
        public override string DisplayName => "Lichen Patch";
        public override string Description => "Colorful growth on rock surface";
        public override List<string> OutcomeKeywords => new() { "lichen", "patch", "colorful", "orange", "yellow", "growing", "crusty", "textured", "ancient", "symbiotic" };
    }
}
