using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class FrozenRavineLipNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 8;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(FrozenRavineFloorNode);
    
    public override string NodeId => "frozen_ravine_lip";
    public override string ContextDescription => "standing at the frozen ravine lip";
    public override string TransitionDescription => "approach the frozen ravine lip";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "ravine", "lip", "edge", "frozen", "precipice", "drop", "ice", "deep", "dangerous", "rim" };
    
    private static readonly string[] Moods = { "precipitous", "frozen", "deep", "dangerous" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} ravine lip";
    }
    
    public sealed class RavineQuartz : Item
    {
        public override string ItemId => "frozen_ravine_lip_ravine_quartz";
        public override string DisplayName => "Ravine Quartz";
        public override string Description => "Milky quartz collectible from the ravine edge";
        public override List<string> OutcomeKeywords => new() { "quartz", "milky", "white", "crystalline", "translucent", "hard", "mineral", "vein", "collectible", "stone" };
    }
    
    public sealed class FrozenRock : Item
    {
        public override string ItemId => "frozen_ravine_lip_frozen_rock";
        public override string DisplayName => "Frozen Rock";
        public override string Description => "Ice-covered rock at edge";
        public override List<string> OutcomeKeywords => new() { "rock", "frozen", "ice", "covered", "edge", "cold", "hard", "coated", "slippery", "glazed" };
    }
    
    public sealed class IcicleFormation : Item
    {
        public override string ItemId => "frozen_ravine_lip_icicle_formation";
        public override string DisplayName => "Icicle Formation";
        public override string Description => "Cluster of icicles hanging over edge";
        public override List<string> OutcomeKeywords => new() { "icicles", "formation", "hanging", "frozen", "cluster", "sharp", "pointed", "crystalline", "translucent", "dangerous" };
    }
}
