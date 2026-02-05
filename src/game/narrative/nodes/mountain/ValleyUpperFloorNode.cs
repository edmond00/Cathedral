using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class ValleyUpperFloorNode : PyramidalFeatureNode
{
    public override int MinAltitude => 8;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(ValleyLowerFloorNode);
    
    public override string NodeId => "valley_upper_floor";
    public override string ContextDescription => "on the wide valley upper floor";
    public override string TransitionDescription => "enter the valley upper floor";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "valley", "floor", "upper", "wide", "flat", "meadow", "open", "gentle", "grass", "spacious" };
    
    private static readonly string[] Moods = { "wide", "open", "spacious", "peaceful" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} valley upper floor";
    }
    
    public sealed class MeadowGrass : Item
    {
        public override string ItemId => "valley_upper_floor_meadow_grass";
        public override string DisplayName => "Meadow Grass";
        public override string Description => "Tall grass covering the valley floor";
        public override List<string> OutcomeKeywords => new() { "meadow", "grass", "tall", "green", "waving", "lush", "soft", "covering", "thick", "abundant" };
    }
    
    public sealed class WildflowerPatch : Item
    {
        public override string ItemId => "valley_upper_floor_wildflower_patch";
        public override string DisplayName => "Wildflower Patch";
        public override string Description => "Colorful flowers in the meadow";
        public override List<string> OutcomeKeywords => new() { "wildflower", "patch", "colorful", "flowers", "blooming", "bright", "scattered", "beautiful", "vibrant", "mixed" };
    }
    
    public sealed class GentleSlope : Item
    {
        public override string ItemId => "valley_upper_floor_gentle_slope";
        public override string DisplayName => "Gentle Slope";
        public override string Description => "Gradual incline across the floor";
        public override List<string> OutcomeKeywords => new() { "gentle", "slope", "gradual", "incline", "easy", "smooth", "rolling", "subtle", "mild", "walking" };
    }
}
