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
    
    public override List<string> NodeKeywords => new() { "flooded", "pool", "sedge", "spongy", "moss", "rotting", "water", "swamp", "mire", "wet" };
    
    private static readonly string[] Moods = { "waterlogged", "marshy", "boggy", "swampy", "sodden", "saturated", "squelching", "quagmire" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} mirewood";
    }
    
    public sealed class BogWater : Item
    {
        public override string ItemId => "mirewood_bog_water";
        public override string DisplayName => "Bog Water";
        public override string Description => "Stagnant water collected from mirewood pools";
        public override List<string> OutcomeKeywords => new() { "water", "bog", "stagnant", "murky", "dark", "still", "brackish", "swampy", "thick", "clouded" };
    }
    
    public sealed class BogPeat : Item
    {
        public override string ItemId => "mirewood_bog_peat";
        public override string DisplayName => "Bog Peat";
        public override string Description => "Dense organic matter from the mirewood bog";
        public override List<string> OutcomeKeywords => new() { "peat", "bog", "organic", "dark", "compressed", "fuel", "wet", "fibrous", "carbon", "dense" };
    }
}
