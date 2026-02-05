using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class UpperLedgeNode : PyramidalFeatureNode
{
    public override int MinAltitude => 1;
    public override int MaxAltitude => 3;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(LowerLedgeNode);
    
    public override string NodeId => "upper_ledge";
    public override string ContextDescription => "standing on the upper stone ledge";
    public override string TransitionDescription => "climb to the upper ledge";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "ledge", "upper", "platform", "stone", "flat", "high", "overlook", "view", "shelf", "perch" };
    
    private static readonly string[] Moods = { "commanding", "elevated", "projecting", "prominent" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} upper ledge";
    }
    
    public sealed class LedgeNest : Item
    {
        public override string ItemId => "upper_ledge_nest";
        public override string DisplayName => "Ledge Nest";
        public override string Description => "Bird nest on the high stone platform";
        public override List<string> OutcomeKeywords => new() { "nest", "bird", "twigs", "high", "sheltered", "occupied", "woven", "perched", "eggs", "feathers" };
    }
    
    public sealed class FlatStone : Item
    {
        public override string ItemId => "upper_ledge_flat_stone";
        public override string DisplayName => "Flat Stone";
        public override string Description => "Smooth stone surface on the ledge";
        public override List<string> OutcomeKeywords => new() { "flat", "smooth", "stone", "surface", "level", "platform", "solid", "stable", "horizontal", "even" };
    }
    
    public sealed class OverhangShadow : Item
    {
        public override string ItemId => "upper_ledge_overhang_shadow";
        public override string DisplayName => "Overhang Shadow";
        public override string Description => "Dark shade cast by the rock above";
        public override List<string> OutcomeKeywords => new() { "shadow", "overhang", "shade", "dark", "sheltered", "cool", "protected", "dim", "shaded", "covered" };
    }
}
