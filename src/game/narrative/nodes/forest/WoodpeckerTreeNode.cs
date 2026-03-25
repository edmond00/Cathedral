using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Woodpecker Tree - A tree actively worked by woodpeckers.
/// </summary>
public class WoodpeckerTreeNode : NarrationNode
{
    public override string NodeId => "woodpecker_tree";
    public override string ContextDescription => "examining the woodpecker tree";
    public override string TransitionDescription => "approach the pecked tree";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "hole", "drumming", "bark", "cavity" };
    
    private static readonly string[] Moods = { "rhythmic", "pecked", "excavated", "riddled", "hollowed", "worked", "tapped", "resonant" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} woodpecker tree";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} woodpecker tree";
    }
    
    public sealed class WoodChips : Item
    {
        public override string ItemId => "woodpecker_chips";
        public override string DisplayName => "Wood Chips";
        public override string Description => "Fresh wood chips from woodpecker excavation";
        public override List<string> OutcomeKeywords => new() { "chip", "wood", "splinter" };
    }
    
    public sealed class BarkFragment : Item
    {
        public override string ItemId => "woodpecker_tree_bark_fragment";
        public override string DisplayName => "Pecked Bark Fragment";
        public override string Description => "Bark pieces loosened by woodpecker drilling";
        public override List<string> OutcomeKeywords => new() { "bark", "hole", "fragment" };
    }
}
