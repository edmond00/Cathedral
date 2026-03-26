using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Shadowwood - Level 8. Dark woodland with lichen-crusted trees.
/// </summary>
public class ShadowwoodNode : NarrationNode
{
    public override string NodeId => "shadowwood";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "moving through shadowwood";
    public override string TransitionDescription => "enter the shadowwood";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "shadow", "lichen", "moss", "gloom" };
    
    private static readonly string[] Moods = { "gloomy", "darkened", "shadowy", "murky", "obscure", "dim", "somber", "dusky" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} shadowwood";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"moving through a {mood} shadowwood";
    }
    
    public sealed class DarkBark : Item
    {
        public override string ItemId => "shadowwood_dark_bark";
        public override string DisplayName => "Dark Bark";
        public override string Description => "Nearly black bark from shadowwood trees";
        public override List<string> OutcomeKeywords => new() { "cortex", "shadow", "gnarl" };
    }
}
