using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Shrub Island - An isolated clump of dense shrubs.
/// Associated with: DenseThicketland
/// </summary>
public class ShrubIslandNode : NarrationNode
{
    public override string NodeId => "shrub_island";
    public override string ContextDescription => "circling the shrub island";
    public override string TransitionDescription => "approach the shrub clump";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "shrub", "clump", "isolation" };
    
    private static readonly string[] Moods = { "dense", "impenetrable", "circular", "isolated", "thick", "clustered", "tight", "rounded" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} shrub island";
    }
    
    public sealed class ShrubBerry : Item
    {
        public override string ItemId => "shrub_berry";
        public override string DisplayName => "Shrub Berry";
        public override string Description => "Small dark berries from the shrub island";
        public override List<string> OutcomeKeywords => new() { "berry", "fruit", "bitterness" };
    }
    
    public sealed class ThornScratch : Item
    {
        public override string ItemId => "shrub_island_thorn";
        public override string DisplayName => "Defensive Thorn";
        public override string Description => "A sharp thorn from the impenetrable shrubs";
        public override List<string> OutcomeKeywords => new() { "thorn", "spine", "defense" };
    }
}
