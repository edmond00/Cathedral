using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Wild Strawberry Patch - A ground-hugging carpet of wild strawberries.
/// </summary>
public class WildStrawberryPatchNode : NarrationNode
{
    public override string NodeId => "wild_strawberry_patch";
    public override string ContextDescription => "picking wild strawberries";
    public override string TransitionDescription => "approach the strawberries";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a tiny red <berry> hidden beneath the leaves"), KeywordInContext.Parse("a long thin <runner> trailing away across the ground"), KeywordInContext.Parse("a small white <flower> still open among the fruit"), KeywordInContext.Parse("a sweet <fragrance> rising from the disturbed patch") };
    
    private static readonly string[] Moods = { "tiny", "sweet", "abundant", "ground-hugging", "fragrant", "delicate", "wild", "productive" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} wild strawberry patch";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"picking from a {mood} wild strawberry patch";
    }
    
    public override List<Item> GetItems() => new() { new WildStrawberry(), new StrawberryRunner(), new StrawberryLeaf() };

    public sealed class WildStrawberry : Item
    {
        public override string ItemId => "wild_strawberry";
        public override string DisplayName => "Wild Strawberry";
        public override string Description => "Tiny, intensely sweet wild strawberries";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a cluster of tiny sweet <berry> fruits from the patch"), KeywordInContext.Parse("the small yellow <seed> dotting the outside of each fruit"), KeywordInContext.Parse("a lingering <fragrance> on the fingers after picking") };
    }
    
    public sealed class StrawberryRunner : Item
    {
        public override string ItemId => "wild_strawberry_runner";
        public override string DisplayName => "Strawberry Runner";
        public override string Description => "A thin stem sending out new plants";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a long wiry <stolon> pulled free from the earth"), KeywordInContext.Parse("a pale flexible <stem> connecting parent plant to offset"), KeywordInContext.Parse("the mechanism of <propagation> visible in the trailing shoot") };
    }
    
    public sealed class StrawberryLeaf : Item
    {
        public override string ItemId => "wild_strawberry_leaf";
        public override string DisplayName => "Strawberry Leaf";
        public override string Description => "Triple-leaflet leaves with serrated edges";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a serrated <leaflet> blade from the compound leaf"), KeywordInContext.Parse("the sharp <serration> along the edge of each leaflet"), KeywordInContext.Parse("a prominent <vein> running the length of the leaf") };
    }
}
