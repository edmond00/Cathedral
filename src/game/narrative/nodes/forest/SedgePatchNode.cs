using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Sedge Patch - Dense growth of water-loving sedges.
/// Associated with: Mirewood
/// </summary>
public class SedgePatchNode : NarrationNode
{
    public override string NodeId => "sedge_patch";
    public override string ContextDescription => "wading through sedges";
    public override string TransitionDescription => "enter the sedge patch";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the triangular <sedge> stem in the water"), KeywordInContext.Parse("the squelching <marsh> ground underfoot"), KeywordInContext.Parse("a sharp-edged <stem> cutting the hand"), KeywordInContext.Parse("a dense <tuft> of sedge rising from the mire") };
    
    private static readonly string[] Moods = { "wet", "dense", "marsh-like", "tufted", "grassy", "water-loving", "squelching", "boggy" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} sedge patch";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"wading through a {mood} sedge patch";
    }
    
    public override List<Item> GetItems() => new() { new SedgeHead(), new SedgeRoot() };

    public sealed class SedgeHead : Item
    {
        public override string ItemId => "sedge_seed_head";
        public override string DisplayName => "Sedge Seed Head";
        public override string Description => "A triangular sedge stem with seed head";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a small <utricle> holding the sedge seed"), KeywordInContext.Parse("the spike-like <inflorescence> at the stem tip") };
    }
    
    public sealed class SedgeRoot : Item
    {
        public override string ItemId => "sedge_patch_rhizome";
        public override string DisplayName => "Sedge Rhizome";
        public override string Description => "A tough underground sedge rhizome";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a horizontal <stolon> running through the mud"), KeywordInContext.Parse("a tough fibrous <root> pulled from the wet ground") };
    }
}
