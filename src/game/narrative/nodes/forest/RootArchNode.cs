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
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the perfect natural <arch> of roots overhead"), KeywordInContext.Parse("a thick curved <root> forming the lintel"), KeywordInContext.Parse("a natural <bridge> spanning the eroded channel"), KeywordInContext.Parse("a remarkable living <sculpture> of wood and earth") };
    
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
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a tough <sinew> of root fiber from the arch"), KeywordInContext.Parse("the smooth <curvature> of the bent root") };
    }
    
    public sealed class BarkRubbing : Item
    {
        public override string ItemId => "root_arch_bark_rubbing";
        public override string DisplayName => "Bark Fragment";
        public override string Description => "A piece of bark worn smooth by passage";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a thin <shaving> from the worn arch bark"), KeywordInContext.Parse("a smooth <polish> from centuries of passing hands") };
    }
}
