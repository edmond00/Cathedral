using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class RockfallCrownNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(DebrisFieldNode);
    
    public override string NodeId => "rockfall_crown";
    public override string ContextDescription => "at the rockfall crown";
    public override string TransitionDescription => "climb to the rockfall crown";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "rockfall", "fracture", "scar", "collapse" };
    
    private static readonly string[] Moods = { "fractured", "collapsed", "scarred", "broken" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} rockfall crown";
    }
    
    public sealed class FractureZone : Item
    {
        public override string ItemId => "rockfall_crown_fracture_zone";
        public override string DisplayName => "Fracture Zone";
        public override string Description => "Area where rock face has split";
        public override List<string> OutcomeKeywords => new() { "fracture", "crack", "fissure" };
    }
    
    public sealed class TeeringBlock : Item
    {
        public override string ItemId => "rockfall_crown_teetering_block";
        public override string DisplayName => "Teetering Block";
        public override string Description => "Large rock ready to fall";
        public override List<string> OutcomeKeywords => new() { "block", "instability", "danger" };
    }
    
    public sealed class FreshScar : Item
    {
        public override string ItemId => "rockfall_crown_fresh_scar";
        public override string DisplayName => "Fresh Scar";
        public override string Description => "Exposed rock where material has fallen";
        public override List<string> OutcomeKeywords => new() { "scar", "exposure", "rawness" };
    }
}
