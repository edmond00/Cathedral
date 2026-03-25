using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Wildwood - Level 11. Chaotic mixed-age forest with uprooted trees.
/// </summary>
public class WildwoodNode : NarrationNode
{
    public override string NodeId => "wildwood";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new BoarArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "navigating the wildwood";
    public override string TransitionDescription => "enter the wildwood";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "wildness", "regrowth", "tangle", "chaos" };
    
    private static readonly string[] Moods = { "chaotic", "wild", "untamed", "disordered", "turbulent", "rugged", "rough", "feral" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} wildwood";
    }
    
    public sealed class UntamedSeeds : Item
    {
        public override string ItemId => "wildwood_untamed_seeds";
        public override string DisplayName => "Untamed Seeds";
        public override string Description => "Wild seeds scattered across the untamed forest";
        public override List<string> OutcomeKeywords => new() { "seed", "pod", "wildness" };
    }
    
    public sealed class WildGrowth : Item
    {
        public override string ItemId => "wildwood_wild_growth";
        public override string DisplayName => "Wild Growth";
        public override string Description => "Uncultivated plant matter from the wildwood";
        public override List<string> OutcomeKeywords => new() { "growth", "wildness", "plant" };
    }
}
