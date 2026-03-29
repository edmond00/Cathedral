using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Open Woodland - Level 1. An airy forest zone with scattered trees and grassy clearings.
/// </summary>
public class OpenWoodlandNode : NarrationNode
{
    public override string NodeId => "open_woodland";
    public override string ContextDescription => "exploring the open woodland";
    public override string TransitionDescription => "move into the open woodland";
    public override bool IsEntryNode => true;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a small grassy <meadow> between the trees"), KeywordInContext.Parse("some bright <wildflower>s scattered in the grass"), KeywordInContext.Parse("the warm <sunshine> unblocked by canopy"), KeywordInContext.Parse("a pleasant <openness> in the air ahead") };
    
    private static readonly string[] Moods = { "peaceful", "sunny", "breezy", "bright", "quiet", "serene", "windswept", "tranquil" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} open woodland";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"exploring a {mood} open woodland";
    }
    
    public override List<Item> GetItems() => new() { new WildGrass() };

    public sealed class WildGrass : Item
    {
        public override string ItemId => "open_woodland_wild_grass";
        public override string DisplayName => "Wild Grass";
        public override string Description => "Long blades of wild grass from the open clearing";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a hollow <culm> of wild grass"), KeywordInContext.Parse("a long bending <blade> of open meadow grass") };
    }
}
