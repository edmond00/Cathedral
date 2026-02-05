using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class FrozenRavineFloorNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 8;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(FrozenRavineLipNode);
    
    public override string NodeId => "frozen_ravine_floor";
    public override string ContextDescription => "standing on the frozen ravine floor";
    public override string TransitionDescription => "descend to the frozen ravine floor";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "ravine", "floor", "frozen", "narrow", "ice", "cold", "shadowed", "deep", "confined", "bottom" };
    
    private static readonly string[] Moods = { "shadowed", "narrow", "confined", "frigid" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} ravine floor";
    }
    
    public sealed class PeakBasalt : Item
    {
        public override string ItemId => "frozen_ravine_floor_peak_basalt";
        public override string DisplayName => "Peak Basalt";
        public override string Description => "Dark volcanic basalt collectible from the ravine floor";
        public override List<string> OutcomeKeywords => new() { "basalt", "volcanic", "igneous", "dark", "dense", "fine-grained", "hard", "black", "heavy", "collectible" };
    }
    
    public sealed class GlacierSilt : Item
    {
        public override string ItemId => "frozen_ravine_floor_glacier_silt";
        public override string DisplayName => "Glacier Silt";
        public override string Description => "Fine glacial sediment collectible from the floor";
        public override List<string> OutcomeKeywords => new() { "silt", "glacial", "fine", "sediment", "powder", "grey", "mineral", "ground", "flour", "collectible" };
    }
}
