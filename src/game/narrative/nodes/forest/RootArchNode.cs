using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Root Arch - Roots forming natural arches over eroded ground.
/// Associated with: RootedForest
/// </summary>
public class RootArchNode : NarrationNode
{
    public override string NodeId => "root_arch";
    public override string ContextDescription => "ducking under root arches";
    public override string TransitionDescription => "pass through the root arch";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "arch", "root", "bridge", "sculpture" };
    
    private static readonly string[] Moods = { "arched", "curved", "sculptural", "natural", "spanning", "suspended", "bridge-like", "overhead" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} root arch";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"ducking under a {mood} root arch";
    }
    
    public sealed class ArchedRoot : Item
    {
        public override string ItemId => "arched_root_piece";
        public override string DisplayName => "Arched Root Piece";
        public override string Description => "A curved piece from the root arch";
        public override List<string> OutcomeKeywords => new() { "sinew", "curvature", "wood" };
    }
    
    public sealed class BarkRubbing : Item
    {
        public override string ItemId => "root_arch_bark_rubbing";
        public override string DisplayName => "Bark Fragment";
        public override string Description => "A piece of bark worn smooth by passage";
        public override List<string> OutcomeKeywords => new() { "shaving", "rubbing", "polish" };
    }
}
