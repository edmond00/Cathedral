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
    
    public override List<string> NodeKeywords => new() { "tall", "fronds", "feathery", "dense", "green", "waist-high", "prehistoric", "lush", "spores", "divided" };
    
    private static readonly string[] Moods = { "tall", "dense", "lush", "prehistoric", "towering", "feathery", "thick", "verdant" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} tall fern stand";
    }
    
    public sealed class FernSpore : Item
    {
        public override string ItemId => "fern_spore";
        public override string DisplayName => "Fern Spores";
        public override string Description => "Fine brown spores from fern undersides";
        public override List<string> OutcomeKeywords => new() { "fine", "brown", "powder", "spores", "dust", "reproductive", "tiny", "microscopic", "scattered", "dry" };
    }
    
    public sealed class FernRhizome : Item
    {
        public override string ItemId => "tall_fern_rhizome";
        public override string DisplayName => "Fern Rhizome";
        public override string Description => "A thick underground fern stem";
        public override List<string> OutcomeKeywords => new() { "thick", "brown", "fibrous", "underground", "stem", "rhizome", "hardy", "root", "spreading", "rough" };
    }
}
