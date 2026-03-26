using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Vine-Draped Growth - Shrubs and saplings covered in climbing vines.
/// Associated with: DenseThicketland
/// </summary>
public class VineDrapedGrowthNode : NarrationNode
{
    public override string NodeId => "vine_draped_growth";
    public override string ContextDescription => "pushing through vine-draped growth";
    public override string TransitionDescription => "enter the vine tangle";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "vine", "tendril", "climbing", "entanglement" };
    
    private static readonly string[] Moods = { "draped", "vine-covered", "entangled", "hanging", "wrapped", "smothered", "festooned", "shrouded" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} vine-draped growth";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"pushing through a {mood} vine-draped growth";
    }
    
    public sealed class VineTendril : Item
    {
        public override string ItemId => "vine_tendril";
        public override string DisplayName => "Vine Tendril";
        public override string Description => "A flexible climbing tendril";
        public override List<string> OutcomeKeywords => new() { "cirrus", "liana", "curl" };
    }
    
    public sealed class VineLeaf : Item
    {
        public override string ItemId => "vine_draped_leaf";
        public override string DisplayName => "Vine Leaf";
        public override string Description => "A broad heart-shaped leaf from the climbing vines";
        public override List<string> OutcomeKeywords => new() { "lamina", "liana", "lobe", "vein" };
    }
}
