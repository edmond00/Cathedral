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
    
    public override List<string> NodeKeywords => new() { "deep", "thick", "layers", "leaves", "accumulated", "brown", "rustling", "ankle-deep", "dry", "buried" };
    
    private static readonly string[] Moods = { "deep", "thick", "accumulated", "layered", "rustling", "abundant", "ankle-deep", "buried" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} deep leaf litter";
    }
    
    public sealed class DriedLeafPile : Item
    {
        public override string ItemId => "dried_leaf_pile";
        public override string DisplayName => "Dried Leaf Pile";
        public override string Description => "A bundle of crisp, layered leaves";
        public override List<string> OutcomeKeywords => new() { "dry", "crisp", "brown", "layers", "leaves", "rustling", "papery", "brittle", "accumulated", "bundle" };
    }
}
