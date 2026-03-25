using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class WindScouredRidgeCrestNode : PyramidalFeatureNode
{
    public override int MinAltitude => 0;
    public override int MaxAltitude => 2;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(WindScouredRidgeFlankedNode);
    
    public override string NodeId => "wind_scoured_ridge_crest";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.25f),
        new NpcEncounterSlot(new SavageArchetype(), spawnChance: 0.10f),
    };
    public override string ContextDescription => "standing on the wind-scoured ridge crest";
    public override string TransitionDescription => "ascend to the wind-scoured ridge crest";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "wind", "ridge", "ice", "barrenness" };
    
    private static readonly string[] Moods = { "howling", "exposed", "barren", "relentless" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} wind-scoured ridge crest";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} wind-scoured ridge crest";
    }
    
    public sealed class AlpineGneiss : Item
    {
        public override string ItemId => "wind_scoured_ridge_crest_alpine_gneiss";
        public override string DisplayName => "Alpine Gneiss";
        public override string Description => "Banded metamorphic rock collectible from the ridge";
        public override List<string> OutcomeKeywords => new() { "gneiss", "metamorphic", "banding" };
    }
    
    public sealed class BareRock : Item
    {
        public override string ItemId => "wind_scoured_ridge_crest_bare_rock";
        public override string DisplayName => "Bare Rock";
        public override string Description => "Rock stripped of all ice by wind";
        public override List<string> OutcomeKeywords => new() { "rock", "wind", "scouring" };
    }
}
