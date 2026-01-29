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
    
    public override List<string> NodeKeywords => new() { "low", "tangled", "branches", "dark", "twisted", "hanging", "interwoven", "catching", "dense", "maze" };
    
    private static readonly string[] Moods = { "tangled", "maze-like", "twisted", "dark", "catching", "dense", "interwoven", "hidden" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} hidden branch tangle";
    }
    
    public sealed class TangledTwig : Item
    {
        public override string ItemId => "tangled_twig";
        public override string DisplayName => "Tangled Twig";
        public override string Description => "A twisted twig from the dark tangle";
        public override List<string> OutcomeKeywords => new() { "twisted", "dark", "bent", "twig", "gnarled", "crooked", "thin", "brittle", "branch", "curved" };
    }
}
