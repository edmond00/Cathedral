using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Sedge Patch - Dense growth of water-loving sedges.
/// Associated with: Mirewood
/// </summary>
public class SedgePatchNode : NarrationNode
{
    public override string NodeId => "sedge_patch";
    public override string ContextDescription => "wading through sedges";
    public override string TransitionDescription => "enter the sedge patch";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "grassy", "triangular", "stems", "wet", "sedge", "dense", "marsh", "water-loving", "green", "tufted" };
    
    private static readonly string[] Moods = { "wet", "dense", "marsh-like", "tufted", "grassy", "water-loving", "squelching", "boggy" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} sedge patch";
    }
    
    public sealed class SedgeHead : Item
    {
        public override string ItemId => "sedge_seed_head";
        public override string DisplayName => "Sedge Seed Head";
        public override string Description => "A triangular sedge stem with seed head";
        public override List<string> OutcomeKeywords => new() { "triangular", "stem", "seeds", "brown", "sedge", "head", "dry", "grass-like", "tufted", "clustered" };
    }
}
