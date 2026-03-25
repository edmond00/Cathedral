using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Tree Sap Flow - A wound in a tree oozing sticky sap.
/// </summary>
public class TreeSapFlowNode : NarrationNode
{
    public override string NodeId => "tree_sap_flow";
    public override string ContextDescription => "examining the sap flow";
    public override string TransitionDescription => "investigate the sap";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "resin", "amber", "sap", "wound" };
    
    private static readonly string[] Moods = { "oozing", "dripping", "sticky", "golden", "amber", "fresh", "crystallizing", "gleaming" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} tree sap flow";
    }
    
    public sealed class TreeResin : Item
    {
        public override string ItemId => "tree_resin";
        public override string DisplayName => "Tree Resin";
        public override string Description => "Sticky amber resin collected from the tree";
        public override List<string> OutcomeKeywords => new() { "resin", "sap", "amber" };
    }
    
    public sealed class CrystallizedSap : Item
    {
        public override string ItemId => "tree_sap_crystallized";
        public override string DisplayName => "Crystallized Sap";
        public override string Description => "Hardened amber crystals of ancient sap";
        public override List<string> OutcomeKeywords => new() { "crystal", "amber", "fossil", "sap" };
    }
}
