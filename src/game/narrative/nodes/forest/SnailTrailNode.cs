using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Snail Trail - A glistening path left by forest snails.
/// </summary>
public class SnailTrailNode : NarrationNode
{
    public override string NodeId => "snail_trail";
    public override string ContextDescription => "following the snail trail";
    public override string TransitionDescription => "follow the slime trail";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "slime", "trail", "spiral", "mucus" };
    
    private static readonly string[] Moods = { "glistening", "silvery", "shimmering", "fresh", "wet", "meandering", "slow", "deliberate" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} snail trail";
    }
    
    public sealed class SnailShell : Item
    {
        public override string ItemId => "empty_snail_shell";
        public override string DisplayName => "Empty Snail Shell";
        public override string Description => "A spiral snail shell, the occupant long gone";
        public override List<string> OutcomeKeywords => new() { "shell", "spiral", "calcium" };
    }
    
    public sealed class SlimeTrail : Item
    {
        public override string ItemId => "snail_trail_slime";
        public override string DisplayName => "Fresh Slime Trail";
        public override string Description => "Glistening mucus trail still wet";
        public override List<string> OutcomeKeywords => new() { "slime", "mucus", "iridescence" };
    }
    
    public sealed class MossClump : Item
    {
        public override string ItemId => "snail_trail_moss_clump";
        public override string DisplayName => "Damp Moss Clump";
        public override string Description => "Small clump of moss where snails graze and rest";
        public override List<string> OutcomeKeywords => new() { "moss", "clump", "cushion" };
    }
}
