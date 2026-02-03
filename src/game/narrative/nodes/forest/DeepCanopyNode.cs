using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Deep Canopy - Level 7. Closed-crown forest with filtered light.
/// </summary>
public class DeepCanopyNode : NarrationNode
{
    public override string NodeId => "deep_canopy";
    public override string ContextDescription => "walking beneath the deep canopy";
    public override string TransitionDescription => "enter the deep canopy";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "closed", "crown", "roots", "stone", "shade", "filtered", "shafts", "dim", "trunk", "carpet" };
    
    private static readonly string[] Moods = { "sheltered", "enclosed", "shadowed", "filtered", "dim", "protected", "covered", "roofed" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} deep canopy";
    }
    
    public sealed class FallenLeaves : Item
    {
        public override string ItemId => "deep_canopy_fallen_leaves";
        public override string DisplayName => "Fallen Leaves";
        public override string Description => "Layers of leaves fallen from the high canopy";
        public override List<string> OutcomeKeywords => new() { "leaves", "fallen", "layers", "brown", "dry", "rustling", "dead", "crisp", "carpet", "accumulated" };
    }
    
    public sealed class CanopySeed : Item
    {
        public override string ItemId => "deep_canopy_canopy_seed";
        public override string DisplayName => "Canopy Seed";
        public override string Description => "Large seed fallen from the high canopy";
        public override List<string> OutcomeKeywords => new() { "seed", "large", "hard", "brown", "woody", "fallen", "tree", "heavy", "oval", "canopy" };
    }
}
