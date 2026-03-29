using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Canopy Gap Line - A transversal feature where light penetrates the forest ceiling.
/// </summary>
public class CanopyGapLineNode : NarrationNode
{
    public override string NodeId => "canopy_gap_line";
    public override string ContextDescription => "walking through the canopy gap";
    public override string TransitionDescription => "follow the light gap";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a column of warm <light> striking the forest floor"), KeywordInContext.Parse("a bright <shaft> of sun cutting through the trees"), KeywordInContext.Parse("the <gap> in the canopy overhead"), KeywordInContext.Parse("the sudden warmth of <sunshine> on the skin") };
    
    private static readonly string[] Moods = { "bright", "sunlit", "illuminated", "radiant", "gleaming", "golden", "dappled", "shimmering" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} canopy gap line";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"walking through a {mood} canopy gap line";
    }
    
    public override List<Item> GetItems() => new() { new SunwarmLeaves(), new YoungShoots() };

    public sealed class SunwarmLeaves : Item
    {
        public override string ItemId => "sunwarm_leaves";
        public override string DisplayName => "Sunwarm Leaves";
        public override string Description => "Fresh leaves warmed by direct sunlight";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a broad <lamina> warmed by direct sun"), KeywordInContext.Parse("some fresh <growth> reaching toward the gap") };
    }
    
    public sealed class YoungShoots : Item
    {
        public override string ItemId => "canopy_gap_line_young_shoots";
        public override string DisplayName => "Young Shoots";
        public override string Description => "Tender plant shoots growing in the sunlight";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("an <apical> bud swelling toward the light"), KeywordInContext.Parse("a pale tender <sapling> rising into the gap") };
    }
}
