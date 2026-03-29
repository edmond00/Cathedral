using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Fallen Giant Trunk - A massive collapsed tree trunk.
/// </summary>
public class FallenGiantTrunkNode : NarrationNode
{
    public override string NodeId => "fallen_giant_trunk";
    public override string ContextDescription => "climbing the fallen giant";
    public override string TransitionDescription => "climb onto the trunk";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the massive fallen <trunk> stretching ahead"), KeywordInContext.Parse("the loose crumbling <bark> peeling away"), KeywordInContext.Parse("the soft <decay> softening the inner wood"), KeywordInContext.Parse("a host of tiny <insects> moving through the rot") };
    
    private static readonly string[] Moods = { "massive", "fallen", "decaying", "moss-covered", "enormous", "ancient", "collapsed", "weathered" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} fallen giant trunk";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"climbing a {mood} fallen giant trunk";
    }
    
    public override List<Item> GetItems() => new() { new BeetleLarva(), new SoftRot() };

    public sealed class BeetleLarva : Item
    {
        public override string ItemId => "beetle_larva";
        public override string DisplayName => "Beetle Larva";
        public override string Description => "A fat white larva from beneath the bark";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a fat white <larva> found under the bark"), KeywordInContext.Parse("a soft pale <grub> curled in the rotting wood") };
    }
    
    public sealed class SoftRot : Item
    {
        public override string ItemId => "fallen_trunk_soft_rot";
        public override string DisplayName => "Soft Rot Wood";
        public override string Description => "Punky, decomposing wood from the trunk interior";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the soft punky <decay> crumbling in the hand"), KeywordInContext.Parse("some white <fungus> threads through the rotten wood") };
    }
}
