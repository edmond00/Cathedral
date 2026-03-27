using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Isolated Plant Cluster - A rare group of plants in the deep forest.
/// Associated with: Deepwood
/// </summary>
public class IsolatedPlantClusterNode : NarrationNode
{
    public override string NodeId => "isolated_plant_cluster";
    public override string ContextDescription => "examining isolated plants";
    public override string TransitionDescription => "approach the plant cluster";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the <isolation> of these plants in the dark forest"), KeywordInContext.Parse("a small <cluster> of plants struggling forward"), KeywordInContext.Parse("a testament to <survival> in deep shadow") };
    
    private static readonly string[] Moods = { "isolated", "rare", "sparse", "struggling", "alone", "few", "tenacious", "surviving" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} isolated plant cluster";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} isolated plant cluster";
    }
    
    public sealed class RarePlant : Item
    {
        public override string ItemId => "rare_deepwood_plant";
        public override string DisplayName => "Rare Deepwood Plant";
        public override string Description => "A specimen from the isolated cluster";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a curious <adaptation> in this unusual plant"), KeywordInContext.Parse("a rare <specimen> from the deep forest cluster") };
    }
    
    public sealed class AdaptedRoot : Item
    {
        public override string ItemId => "isolated_plant_adapted_root";
        public override string DisplayName => "Adapted Root";
        public override string Description => "A specialized root structure showing survival adaptation";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a reaching <tendril> questing through the soil"), KeywordInContext.Parse("the specialist <adaptation> of this root structure") };
    }
}
