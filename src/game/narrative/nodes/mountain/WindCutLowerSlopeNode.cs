using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class WindCutLowerSlopeNode : PyramidalFeatureNode
{
    public override int MinAltitude => 2;
    public override int MaxAltitude => 6;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(WindCutUpperSlopeNode);
    
    public override string NodeId => "wind_cut_lower_slope";
    public override string ContextDescription => "on the wind-cut lower slope";
    public override string TransitionDescription => "descend to the wind-cut lower slope";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "slope", "shelter", "vegetation", "protection" };
    
    private static readonly string[] Moods = { "sheltered", "protected", "descending", "gentler" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} wind-cut lower slope";
    }
    
    public sealed class AccumulatedDebris : Item
    {
        public override string ItemId => "wind_cut_lower_slope_accumulated_debris";
        public override string DisplayName => "Accumulated Debris";
        public override string Description => "Material deposited by wind from above";
        public override List<string> OutcomeKeywords => new() { "debris", "wind", "drift" };
    }
    
    public sealed class ShelterStone : Item
    {
        public override string ItemId => "wind_cut_lower_slope_shelter_stone";
        public override string DisplayName => "Shelter Stone";
        public override string Description => "Boulder providing wind protection";
        public override List<string> OutcomeKeywords => new() { "shelter", "boulder", "windbreak" };
    }
}
