using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class DebrisFieldNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(RockfallCrownNode);
    
    public override string NodeId => "debris_field";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new BearArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "in the debris field";
    public override string TransitionDescription => "descend to the debris field";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the scattered <debris> of an ancient collapse"), KeywordInContext.Parse("a moss-covered <boulder> blocking the path"), KeywordInContext.Parse("a jagged <rock> jutting from the field"), KeywordInContext.Parse("the utter <chaos> of the fallen stone") };
    
    private static readonly string[] Moods = { "chaotic", "scattered", "jumbled", "devastated" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} debris field";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"in a {mood} debris field";
    }
    
    public sealed class LargeBoulder : Item
    {
        public override string ItemId => "debris_field_large_boulder";
        public override string DisplayName => "Large Boulder";
        public override string Description => "Massive rock fallen from above";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a glacial <erratic> dropped far from its source"), KeywordInContext.Parse("a fractured <rock> split on impact"), KeywordInContext.Parse("the sheer <mass> of the fallen stone") };
    }
    
    public sealed class RockDust : Item
    {
        public override string ItemId => "debris_field_rock_dust";
        public override string DisplayName => "Rock Dust";
        public override string Description => "Fine powder from the impact";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a fine <silt> coating the debris field"), KeywordInContext.Parse("a pale <powder> of pulverised stone"), KeywordInContext.Parse("a dark <mineral> seam in the rubble") };
    }
}
