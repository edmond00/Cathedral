using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Mirewood - Level 14. Flooded forest with shallow pools and sedges.
/// </summary>
public class MirewoodNode : NarrationNode
{
    public override string NodeId => "mirewood";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.40f),
        new NpcEncounterSlot(new BoarArchetype(), spawnChance: 0.30f),
    };
    public override string ContextDescription => "wading through mirewood";
    public override string TransitionDescription => "wade into the mirewood";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a dark shallow <pool> between the trunks"), KeywordInContext.Parse("some <sedge> tufts rising from standing water"), KeywordInContext.Parse("the squelching ground of this flooded <swamp>"), KeywordInContext.Parse("the clinging <mire> underfoot refusing each step") };
    
    private static readonly string[] Moods = { "waterlogged", "marshy", "boggy", "swampy", "sodden", "saturated", "squelching", "quagmire" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} mirewood";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"wading through a {mood} mirewood";
    }
    
    public sealed class BogWater : Item
    {
        public override string ItemId => "mirewood_bog_water";
        public override string DisplayName => "Bog Water";
        public override string Description => "Stagnant water collected from mirewood pools";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the dark <leachate> from the bog filtered into the pool"), KeywordInContext.Parse("the foul <stagnation> smell of the mirewood water") };
    }
    
    public sealed class BogPeat : Item
    {
        public override string ItemId => "mirewood_bog_peat";
        public override string DisplayName => "Bog Peat";
        public override string Description => "Dense organic matter from the mirewood bog";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the dark compacted <humus> of the bog layer"), KeywordInContext.Parse("the dense <carbon>-rich peat in the hand") };
    }
}
