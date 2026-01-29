using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Leaf-Litter Hollow - A depression filled with decomposing leaves.
/// Associated with: Greenwood
/// </summary>
public class LeafLitterHollowNode : NarrationNode
{
    public override string NodeId => "leaf_litter_hollow";
    public override string ContextDescription => "exploring the leaf litter";
    public override string TransitionDescription => "descend into the hollow";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "depression", "leaves", "decomposing", "deep", "soft", "brown", "mulch", "organic", "moist", "layer" };
    
    private static readonly string[] Moods = { "deep", "soft", "decomposing", "layered", "accumulated", "moist", "rich", "organic" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} leaf-litter hollow";
    }
    
    public sealed class LeafMold : Item
    {
        public override string ItemId => "leaf_mold";
        public override string DisplayName => "Leaf Mold";
        public override string Description => "Rich, partially decomposed leaf matter";
        public override List<string> OutcomeKeywords => new() { "dark", "crumbly", "rich", "organic", "decomposed", "humus", "fertile", "moist", "earthy", "mold" };
    }
}
