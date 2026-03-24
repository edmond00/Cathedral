using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Beech Stand - A grove of smooth-barked beech trees.
/// Associated with: Brightwood
/// </summary>
public class BeechStandNode : NarrationNode
{
    public override string NodeId => "beech_stand";
    public override string ContextDescription => "walking through the beech stand";
    public override string TransitionDescription => "enter the beeches";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "smooth", "grey", "bark", "tall", "elegant", "mast", "nuts", "copper", "leaves", "silver" };
    
    private static readonly string[] Moods = { "elegant", "silvery", "stately", "smooth-barked", "graceful", "noble", "towering", "luminous" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} beech stand";
    }
    
    public sealed class Beechnut : Item
    {
        public override string ItemId => "beechnut";
        public override string DisplayName => "Beechnut";
        public override string Description => "A triangular beechnut in its spiny husk";
        public override List<string> OutcomeKeywords => new() { "triangular", "brown", "husk", "spiny", "shell", "nut", "edible", "small", "mast", "kernel" };
    }
    
    public sealed class BeechLeaf : Item
    {
        public override string ItemId => "beech_stand_copper_leaf";
        public override string DisplayName => "Copper Beech Leaf";
        public override string Description => "A copper-colored beech leaf with delicate veins";
        public override List<string> OutcomeKeywords => new() { "copper", "oval", "veined", "thin", "papery", "wavy", "edges", "glossy", "bronze", "autumn" };
    }
}
