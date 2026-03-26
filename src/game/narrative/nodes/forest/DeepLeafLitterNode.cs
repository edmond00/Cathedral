using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Deep Leaf Litter - Extremely thick accumulation of fallen leaves.
/// Associated with: Deepwood
/// </summary>
public class DeepLeafLitterNode : NarrationNode
{
    public override string NodeId => "deep_leaf_litter";
    public override string ContextDescription => "wading through deep leaf litter";
    public override string TransitionDescription => "push into the deep litter";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "leaf", "litter", "depth", "layers" };
    
    private static readonly string[] Moods = { "deep", "thick", "accumulated", "layered", "rustling", "abundant", "ankle-deep", "buried" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} deep leaf litter";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"wading through a {mood} deep leaf litter";
    }
    
    public sealed class DriedLeafPile : Item
    {
        public override string ItemId => "dried_leaf_pile";
        public override string DisplayName => "Dried Leaf Pile";
        public override string Description => "A bundle of crisp, layered leaves";
        public override List<string> OutcomeKeywords => new() { "lamina", "bundle", "rustling" };
    }
    
    public sealed class HiddenAcorn : Item
    {
        public override string ItemId => "deep_litter_hidden_acorn";
        public override string DisplayName => "Hidden Acorn";
        public override string Description => "An acorn buried deep beneath the leaf layers";
        public override List<string> OutcomeKeywords => new() { "cupule", "cap", "cache" };
    }
}
