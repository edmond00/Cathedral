using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Dense Thicketland - Level 10. Impenetrable shrub walls and vine growth.
/// </summary>
public class DenseThicketlandNode : NarrationNode
{
    public override string NodeId => "dense_thicketland";
    public override string ContextDescription => "pushing through dense thicketland";
    public override string TransitionDescription => "force into the thicketland";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "thorn", "shrub", "vine", "thicket" };
    
    private static readonly string[] Moods = { "impenetrable", "tangled", "maze-like", "interwoven", "cluttered", "blocked", "choked", "knotted" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} dense thicketland";
    }
    
    public sealed class ThornedBranch : Item
    {
        public override string ItemId => "dense_thicketland_thorned_branch";
        public override string DisplayName => "Thorned Branch";
        public override string Description => "Spiky branch broken from the dense thicket";
        public override List<string> OutcomeKeywords => new() { "thorn", "branch", "bramble" };
    }
    
    public sealed class ThicketBerries : Item
    {
        public override string ItemId => "dense_thicketland_thicket_berries";
        public override string DisplayName => "Thicket Berries";
        public override string Description => "Small dark berries hidden in the thicket";
        public override List<string> OutcomeKeywords => new() { "berry", "fruit", "bramble" };
    }
}
