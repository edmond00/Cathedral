using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Tall Fern Stand - Dense growth of waist-high ferns.
/// Associated with: MixedUnderwood
/// </summary>
public class TallFernStandNode : NarrationNode
{
    public override string NodeId => "tall_fern_stand";
    public override string ContextDescription => "wading through tall ferns";
    public override string TransitionDescription => "enter the fern stand";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a tall arching <frond> as high as the shoulder"), KeywordInContext.Parse("some brown <spore> clusters on the underside"), KeywordInContext.Parse("a sense of living <prehistory> in the fern forest"), KeywordInContext.Parse("an overwhelming <lushness> of massed green fronds") };
    
    private static readonly string[] Moods = { "tall", "dense", "lush", "prehistoric", "towering", "feathery", "thick", "verdant" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} tall fern stand";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"wading through a {mood} tall fern stand";
    }
    
    public sealed class FernSpore : Item
    {
        public override string ItemId => "fern_spore";
        public override string DisplayName => "Fern Spores";
        public override string Description => "Fine brown spores from fern undersides";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a burst <sporangium> from the fern underside"), KeywordInContext.Parse("a cloud of brown spore <dust> rising from a disturbed frond") };
    }
    
    public sealed class FernRhizome : Item
    {
        public override string ItemId => "tall_fern_rhizome";
        public override string DisplayName => "Fern Rhizome";
        public override string Description => "A thick underground fern stem";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a creeping underground <stolon> from the fern"), KeywordInContext.Parse("a thick fibrous fern <root> pulled from below") };
    }
}
