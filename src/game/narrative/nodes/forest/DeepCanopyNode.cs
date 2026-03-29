using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Deep Canopy - Level 7. Closed-crown forest with filtered light.
/// </summary>
public class DeepCanopyNode : NarrationNode
{
    public override string NodeId => "deep_canopy";
    public override string ContextDescription => "walking beneath the deep canopy";
    public override string TransitionDescription => "enter the deep canopy";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the interlocked <crown>s far overhead"), KeywordInContext.Parse("the deep <shade> beneath the closed canopy"), KeywordInContext.Parse("some buttressed <roots> spreading across the ground"), KeywordInContext.Parse("a column-like <trunk> rising into the dark") };
    
    private static readonly string[] Moods = { "sheltered", "enclosed", "shadowed", "filtered", "dim", "protected", "covered", "roofed" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} deep canopy";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"walking beneath a {mood} deep canopy";
    }
    
    public override List<Item> GetItems() => new() { new FallenLeaves(), new CanopySeed() };

    public sealed class FallenLeaves : Item
    {
        public override string ItemId => "deep_canopy_fallen_leaves";
        public override string DisplayName => "Fallen Leaves";
        public override string Description => "Layers of leaves fallen from the high canopy";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a broad dark <lamina> from the high canopy"), KeywordInContext.Parse("a deep <layer> of accumulated fallen leaves") };
    }
    
    public sealed class CanopySeed : Item
    {
        public override string ItemId => "deep_canopy_canopy_seed";
        public override string DisplayName => "Canopy Seed";
        public override string Description => "Large seed fallen from the high canopy";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a large <propagule> fallen from the high canopy"), KeywordInContext.Parse("a heavy <emergent> seed hitting the ground") };
    }
}
