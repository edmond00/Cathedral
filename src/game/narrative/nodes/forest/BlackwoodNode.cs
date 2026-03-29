using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Blackwood - Level 15. Lightless forest with dense trunk walls and decay.
/// </summary>
public class BlackwoodNode : NarrationNode
{
    public override string NodeId => "blackwood";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.45f),
        new NpcEncounterSlot(new BearArchetype(), spawnChance: 0.30f),
    };
    public override string ContextDescription => "feeling through blackwood";
    public override string TransitionDescription => "enter the blackwood";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a tangle of grey <deadwood> blocking the way"), KeywordInContext.Parse("the deep <darkness> between the trunk walls"), KeywordInContext.Parse("a rotting <heap> of collapsed branches") };
    
    private static readonly string[] Moods = { "lightless", "pitch-dark", "oppressive", "suffocating", "impenetrable", "black", "void-like", "abyssal" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} blackwood";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"feeling through a {mood} blackwood";
    }
    
    public override List<Item> GetItems() => new() { new CharredTwigs() };

    public sealed class CharredTwigs : Item
    {
        public override string ItemId => "blackwood_charred_twigs";
        public override string DisplayName => "Charred Twigs";
        public override string Description => "Blackened twigs from the scorched blackwood";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a stick of black <charcoal> from a scorched branch"), KeywordInContext.Parse("some grey <ash> caked into the cracked wood"), KeywordInContext.Parse("a sharp <splinter> of blackened wood") };
    }
}
