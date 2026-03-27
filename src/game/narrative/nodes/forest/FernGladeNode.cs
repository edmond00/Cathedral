using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Fern Glade - A sunlit clearing filled with ferns.
/// </summary>
public class FernGladeNode : NarrationNode
{
    public override string NodeId => "fern_glade";
    public override string ContextDescription => "walking through the fern glade";
    public override string TransitionDescription => "enter the ferns";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a broad <frond> uncurling in the sunlight"), KeywordInContext.Parse("some brown <spore> clusters on the underside"), KeywordInContext.Parse("a dense stand of bright green <fern>"), KeywordInContext.Parse("the open sunlit <glade> ahead") };
    
    private static readonly string[] Moods = { "lush", "green", "feathery", "dense", "verdant", "delicate", "thriving", "prehistoric" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} fern glade";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"walking through a {mood} fern glade";
    }
    
    public sealed class FernFrond : Item
    {
        public override string ItemId => "fern_frond";
        public override string DisplayName => "Fern Frond";
        public override string Description => "A fresh fern frond with delicate leaflets";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a tiny <pinnule> at the tip of the frond"), KeywordInContext.Parse("the central <rachis> running up the frond") };
    }
    
    public sealed class GladeFernSpore : Item
    {
        public override string ItemId => "fern_glade_spore_patch";
        public override string DisplayName => "Spore Patch";
        public override string Description => "Undersides of mature fronds covered in brown spores";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the brown <spore> patches on the frond underside"), KeywordInContext.Parse("a burst <capsule> releasing fine spore dust") };
    }
}
