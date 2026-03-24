using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class WindCutUpperSlopeNode : PyramidalFeatureNode
{
    public override int MinAltitude => 2;
    public override int MaxAltitude => 6;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(WindCutLowerSlopeNode);
    
    public override string NodeId => "wind_cut_upper_slope";
    public override string ContextDescription => "on the wind-cut upper slope";
    public override string TransitionDescription => "climb to the wind-cut upper slope";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "wind", "slope", "exposed", "eroded", "bare", "scoured", "steep", "weathered", "barren", "harsh" };
    
    private static readonly string[] Moods = { "wind-scoured", "barren", "exposed", "harsh" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} wind-cut upper slope";
    }
    
    public sealed class ErodedRock : Item
    {
        public override string ItemId => "wind_cut_upper_slope_eroded_rock";
        public override string DisplayName => "Eroded Rock";
        public override string Description => "Stone worn smooth by constant wind";
        public override List<string> OutcomeKeywords => new() { "eroded", "smooth", "wind", "worn", "polished", "shaped", "weathered", "rounded", "carved", "sculpted" };
    }
    
    public sealed class DustDevil : Item
    {
        public override string ItemId => "wind_cut_upper_slope_dust_devil";
        public override string DisplayName => "Dust Devil";
        public override string Description => "Small whirlwind of dust and debris";
        public override List<string> OutcomeKeywords => new() { "dust", "devil", "whirlwind", "spinning", "debris", "swirling", "vortex", "moving", "cyclone", "twister" };
    }
    
    public sealed class BareGround : Item
    {
        public override string ItemId => "wind_cut_upper_slope_bare_ground";
        public override string DisplayName => "Bare Ground";
        public override string Description => "Soil stripped away by wind erosion";
        public override List<string> OutcomeKeywords => new() { "bare", "ground", "exposed", "stripped", "soil", "barren", "naked", "scoured", "empty", "desolate" };
    }
}
