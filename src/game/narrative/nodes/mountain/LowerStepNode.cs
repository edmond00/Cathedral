using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class LowerStepNode : PyramidalFeatureNode
{
    public override int MinAltitude => 4;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(UpperStepNode);
    
    public override string NodeId => "lower_step";
    public override string ContextDescription => "on the lower stone step terrace";
    public override string TransitionDescription => "descend to the lower step";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "terrace", "step", "lower", "bench", "platform", "wide", "sheltered", "base", "foundation", "level" };
    
    private static readonly string[] Moods = { "wide", "sheltered", "stable", "foundational" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} lower step";
    }
    
    public sealed class StoneSlabs : Item
    {
        public override string ItemId => "lower_step_stone_slabs";
        public override string DisplayName => "Stone Slabs";
        public override string Description => "Large flat rocks on the terrace";
        public override List<string> OutcomeKeywords => new() { "slabs", "stone", "flat", "large", "horizontal", "layers", "solid", "thick", "stable", "massive" };
    }
    
    public sealed class SoilPocket : Item
    {
        public override string ItemId => "lower_step_soil_pocket";
        public override string DisplayName => "Soil Pocket";
        public override string Description => "Small patch of earth with vegetation";
        public override List<string> OutcomeKeywords => new() { "soil", "pocket", "earth", "vegetation", "small", "growing", "green", "life", "plants", "fertile" };
    }
}
