using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class LowerScreeNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(UpperScreeNode);
    
    public override string NodeId => "lower_scree";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new SavageArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "on the lower scree slope";
    public override string TransitionDescription => "descend to the lower scree";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "scree", "debris", "gravel", "deposit" };
    
    private static readonly string[] Moods = { "accumulated", "settled", "deposited", "loose" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} lower scree";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} lower scree";
    }
    
    public sealed class ScreeGravel : Item
    {
        public override string ItemId => "scree_gravel";
        public override string DisplayName => "Fine Gravel";
        public override string Description => "Small stones settled at the base";
        public override List<string> OutcomeKeywords => new() { "pebble", "stone", "deposit" };
    }
    
    public sealed class BuriedRock : Item
    {
        public override string ItemId => "lower_scree_buried_rock";
        public override string DisplayName => "Buried Rock";
        public override string Description => "Large stone partially covered by scree";
        public override List<string> OutcomeKeywords => new() { "lithic", "burial", "anchor" };
    }
}
