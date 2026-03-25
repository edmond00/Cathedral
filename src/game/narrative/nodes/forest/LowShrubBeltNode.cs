using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Low Shrub Belt - Dense low shrubs in open woodland.
/// Associated with: OpenWoodland
/// </summary>
public class LowShrubBeltNode : NarrationNode
{
    public override string NodeId => "low_shrub_belt";
    public override string ContextDescription => "pushing through the low shrubs";
    public override string TransitionDescription => "enter the shrub belt";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "twig", "branch", "leaves", "belt" };
    
    private static readonly string[] Moods = { "dense", "tangled", "bushy", "thick", "crowded", "vigorous", "lush", "low-lying" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} low shrub belt";
    }
    
    public sealed class ShrubTwig : Item
    {
        public override string ItemId => "shrub_twig";
        public override string DisplayName => "Shrub Twig";
        public override string Description => "A flexible twig from the shrub belt";
        public override List<string> OutcomeKeywords => new() { "twig", "branch", "wood" };
    }
    
    public sealed class BerryCluster : Item
    {
        public override string ItemId => "low_shrub_berry_cluster";
        public override string DisplayName => "Wild Berry Cluster";
        public override string Description => "Small red berries hanging from a shrub";
        public override List<string> OutcomeKeywords => new() { "berry", "cluster", "ripeness" };
    }
}
