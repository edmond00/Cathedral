using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class FoothillUpperRiseNode : PyramidalFeatureNode
{
    public override int MinAltitude => 9;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(FoothillLowerSlopeNode);
    
    public override string NodeId => "foothill_upper_rise";
    public override string ContextDescription => "on the foothill upper rise";
    public override string TransitionDescription => "climb to the foothill upper rise";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "foothill", "rise", "slope", "grass" };
    
    private static readonly string[] Moods = { "rolling", "gentle", "grassy", "elevated" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} foothill upper rise";
    }
    
    public sealed class RollingGrass : Item
    {
        public override string ItemId => "foothill_upper_rise_rolling_grass";
        public override string DisplayName => "Rolling Grass";
        public override string Description => "Grass covering the gentle slopes";
        public override List<string> OutcomeKeywords => new() { "grass", "slope", "meadow" };
    }
    
    public sealed class ScatteredShrubs : Item
    {
        public override string ItemId => "foothill_upper_rise_scattered_shrubs";
        public override string DisplayName => "Scattered Shrubs";
        public override string Description => "Bushes dotting the hillside";
        public override List<string> OutcomeKeywords => new() { "shrub", "bush", "vegetation" };
    }
    
    public sealed class ViewUpward : Item
    {
        public override string ItemId => "foothill_upper_rise_view_upward";
        public override string DisplayName => "Upward View";
        public override string Description => "Sight of higher peaks above";
        public override List<string> OutcomeKeywords => new() { "peak", "vista", "mountain" };
    }
}
