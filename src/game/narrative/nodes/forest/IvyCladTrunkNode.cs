using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Ivy-Clad Trunk - A tree wrapped in climbing ivy.
/// Associated with: Greenwood
/// </summary>
public class IvyCladTrunkNode : NarrationNode
{
    public override string NodeId => "ivy_clad_trunk";
    public override string ContextDescription => "examining the ivy-covered trunk";
    public override string TransitionDescription => "approach the ivy trunk";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "vine", "leaves", "trunk", "tenacity" };
    
    private static readonly string[] Moods = { "vine-covered", "wrapped", "engulfed", "cloaked", "draped", "smothered", "verdant", "encased" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} ivy-clad trunk";
    }
    
    public sealed class IvyLeaf : Item
    {
        public override string ItemId => "ivy_leaf";
        public override string DisplayName => "Ivy Leaf";
        public override string Description => "A glossy, lobed ivy leaf";
        public override List<string> OutcomeKeywords => new() { "leaf", "lobe", "vein", "wax" };
    }
    
    public sealed class AerialRoot : Item
    {
        public override string ItemId => "ivy_trunk_aerial_root";
        public override string DisplayName => "Aerial Root";
        public override string Description => "Clinging ivy roots attached to bark";
        public override List<string> OutcomeKeywords => new() { "rootlet", "adhesion", "fiber" };
    }
}
