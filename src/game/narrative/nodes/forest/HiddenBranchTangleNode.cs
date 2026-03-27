using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Hidden Branch Tangle - Low-hanging tangled branches in darkness.
/// Associated with: Shadowwood
/// </summary>
public class HiddenBranchTangleNode : NarrationNode
{
    public override string NodeId => "hidden_branch_tangle";
    public override string ContextDescription => "ducking through branch tangles";
    public override string TransitionDescription => "navigate the tangles";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a low <branch> catching the shoulder"), KeywordInContext.Parse("the disorienting <tangle> of crossing limbs"), KeywordInContext.Parse("the deep <darkness> between interlocked boughs"), KeywordInContext.Parse("this branching <maze> with no clear path") };
    
    private static readonly string[] Moods = { "tangled", "maze-like", "twisted", "dark", "catching", "dense", "interwoven", "hidden" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} hidden branch tangle";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"ducking through a {mood} hidden branch tangle";
    }
    
    public sealed class TangledTwig : Item
    {
        public override string ItemId => "tangled_twig";
        public override string DisplayName => "Tangled Twig";
        public override string Description => "A twisted twig from the dark tangle";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a tight <knot> in the twisted branch"), KeywordInContext.Parse("a dark <gnarl> in the wood's surface") };
    }
    
    public sealed class CobwebVeil : Item
    {
        public override string ItemId => "hidden_tangle_cobweb";
        public override string DisplayName => "Cobweb Veil";
        public override string Description => "Thick spider webs draped across the branches";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a veil of spider <silk> across the branches"), KeywordInContext.Parse("a fine <gossamer> web draped over the tangle") };
    }
}
