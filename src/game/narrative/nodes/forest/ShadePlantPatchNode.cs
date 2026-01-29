using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Shade Plant Patch - Specialized plants adapted to deep shade.
/// Associated with: DeepCanopy
/// </summary>
public class ShadePlantPatchNode : NarrationNode
{
    public override string NodeId => "shade_plant_patch";
    public override string ContextDescription => "examining shade-loving plants";
    public override string TransitionDescription => "investigate the shade plants";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "dark", "green", "adapted", "low", "broad", "leaves", "shade", "tolerant", "dim", "specialized" };
    
    private static readonly string[] Moods = { "adapted", "shade-loving", "dim", "specialized", "dark-green", "tolerant", "low-growing", "sparse" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} shade plant patch";
    }
    
    public sealed class BroadLeaf : Item
    {
        public override string ItemId => "shade_broad_leaf";
        public override string DisplayName => "Broad Shade Leaf";
        public override string Description => "A wide, dark green leaf from a shade plant";
        public override List<string> OutcomeKeywords => new() { "broad", "dark", "green", "wide", "flat", "leaf", "smooth", "large", "shade", "adapted" };
    }
}
