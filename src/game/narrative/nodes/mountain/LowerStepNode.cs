using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class LowerStepNode : PyramidalFeatureNode
{
    public override int MinAltitude => 4;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(UpperStepNode);
    
    public override string NodeId => "lower_step";
    public override string ContextDescription => "on the lower stone step terrace";
    public override string TransitionDescription => "descend to the lower step";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a wide stone <terrace> cut into the slope"), KeywordInContext.Parse("a flat rock <platform> offering rest"), KeywordInContext.Parse("a natural stone <bench> at the step edge"), KeywordInContext.Parse("the solid rock <foundation> of the terrace") };
    
    private static readonly string[] Moods = { "wide", "sheltered", "stable", "foundational" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} lower step";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} lower step";
    }
    
    public override List<Item> GetItems() => new() { new StoneSlabs(), new SoilPocket() };

    public sealed class StoneSlabs : Item
    {
        public override string ItemId => "lower_step_stone_slabs";
        public override string DisplayName => "Stone Slabs";
        public override string Description => "Large flat rocks on the terrace";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a broad <slab> forming the terrace floor"), KeywordInContext.Parse("a flat <flagstone> laid by frost and time"), KeywordInContext.Parse("a visible <layer> of bedded rock") };
    }
    
    public sealed class SoilPocket : Item
    {
        public override string ItemId => "lower_step_soil_pocket";
        public override string DisplayName => "Soil Pocket";
        public override string Description => "Small patch of earth with vegetation";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a patch of dark <loam> between the rocks"), KeywordInContext.Parse("a sheltered <niche> collecting windblown soil"), KeywordInContext.Parse("a handful of rich <earth> in the rock hollow") };
    }
}
