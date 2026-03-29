using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Brightwood - Level 2. Light-filled woodland with beech trees and ferns.
/// </summary>
public class BrightwoodNode : NarrationNode
{
    public override string NodeId => "brightwood";
    public override string ContextDescription => "wandering through brightwood";
    public override string TransitionDescription => "enter the brightwood";
    public override bool IsEntryNode => true;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a tall smooth-barked <beech> rising above"), KeywordInContext.Parse("some feathery <fern>s carpeting the ground"), KeywordInContext.Parse("the clear <light> filtering through the canopy"), KeywordInContext.Parse("a warm <brightness> touching the leaf-tips") };
    
    private static readonly string[] Moods = { "radiant", "gleaming", "shimmering", "bright", "fresh", "vibrant", "golden", "cheerful" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"{mood} brightwood";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"wandering through a {mood} brightwood";
    }
    
    public override List<Item> GetItems() => new() { new BeechLeaves() };

    public sealed class BeechLeaves : Item
    {
        public override string ItemId => "brightwood_beech_leaves";
        public override string DisplayName => "Beech Leaves";
        public override string Description => "Golden-green leaves from brightwood beech trees";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the flat <lamina> of the beech leaf"), KeywordInContext.Parse("a golden-green <fagus> leaf still soft"), KeywordInContext.Parse("a delicate <vein> pressed through the lamina") };
    }
}
