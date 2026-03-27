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
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a raw sense of <wildness> where no hand has shaped things"), KeywordInContext.Parse("the aggressive <regrowth> of brush after old trees fell"), KeywordInContext.Parse("a <tangle> of branches and roots blocking the way forward"), KeywordInContext.Parse("the productive <chaos> of a forest answering to no order") };
    
    private static readonly string[] Moods = { "chaotic", "wild", "untamed", "disordered", "turbulent", "rugged", "rough", "feral" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} wildwood";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"navigating a {mood} wildwood";
    }
    
    public sealed class UntamedSeeds : Item
    {
        public override string ItemId => "wildwood_untamed_seeds";
        public override string DisplayName => "Untamed Seeds";
        public override string Description => "Wild seeds scattered across the untamed forest";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a hard <kernel> inside a wild seed found on the ground"), KeywordInContext.Parse("a split dry <pod> still holding some seeds within"), KeywordInContext.Parse("a scatter of seeds embodying the <wildness> of the place") };
    }
    
}
