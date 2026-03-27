using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Fungal Ring - A mysterious circle of mushrooms.
/// </summary>
public class FungalRingNode : NarrationNode
{
    public override string NodeId => "fungal_ring";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new DruidArchetype(), spawnChance: 0.20f),
    };
    public override string ContextDescription => "observing the fungal ring";
    public override string TransitionDescription => "approach the ring";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the perfect <arc> of mushrooms in the grass"), KeywordInContext.Parse("some pale white <mushroom>s in a ring"), KeywordInContext.Parse("the hidden <mycelium> network beneath the ring"), KeywordInContext.Parse("a faint <spore> smell rising from disturbed caps") };
    
    private static readonly string[] Moods = { "mysterious", "perfect", "uncanny", "circular", "strange", "symmetrical", "eerie", "magical" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} fungal ring";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"observing a {mood} fungal ring";
    }
    
    public sealed class FairyRingMushroom : Item
    {
        public override string ItemId => "fairy_ring_mushroom";
        public override string DisplayName => "Fairy Ring Mushroom";
        public override string Description => "A white mushroom from the mysterious circle";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a pale <basidiocarp> from the fairy ring"), KeywordInContext.Parse("the flat white <cap> of the ring mushroom"), KeywordInContext.Parse("the fine pink <gill>s on the underside") };
    }
    
    public sealed class MyceliumThread : Item
    {
        public override string ItemId => "fungal_ring_mycelium";
        public override string DisplayName => "Mycelium Thread";
        public override string Description => "White fungal threads just beneath the soil";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some white <hyphae> beneath the topsoil"), KeywordInContext.Parse("a fine <strand> of mycelium on the finger") };
    }
    
    public sealed class SporeCloud : Item
    {
        public override string ItemId => "fungal_ring_spore_cloud";
        public override string DisplayName => "Spore Cloud Sample";
        public override string Description => "A faint puff of microscopic spores released from disturbed caps";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a burst <sporangium> releasing a faint puff"), KeywordInContext.Parse("a fine <powder> of spores drifting in the still air") };
    }
}
