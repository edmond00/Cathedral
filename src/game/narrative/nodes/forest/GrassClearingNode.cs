using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Grass Clearing - A small grassy opening in the woodland.
/// Associated with: OpenWoodland
/// </summary>
public class GrassClearingNode : NarrationNode
{
    public override string NodeId => "grass_clearing";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.25f),
    };
    public override string ContextDescription => "standing in the grass clearing";
    public override string TransitionDescription => "step into the clearing";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "grass", "meadow", "blade", "breeze" };
    
    private static readonly string[] Moods = { "sun-drenched", "breezy", "peaceful", "swaying", "green", "open", "bright", "fresh" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} grass clearing";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing in a {mood} grass clearing";
    }
    
    public sealed class GrassSeed : Item
    {
        public override string ItemId => "grass_seed";
        public override string DisplayName => "Grass Seed";
        public override string Description => "Small seeds from wild grasses";
        public override List<string> OutcomeKeywords => new() { "seed", "grain", "chaff" };
    }
    
    public sealed class GrassFlower : Item
    {
        public override string ItemId => "grass_clearing_flower_head";
        public override string DisplayName => "Grass Flower Head";
        public override string Description => "A delicate grass flower panicle";
        public override List<string> OutcomeKeywords => new() { "panicle", "stem", "pollen" };
    }
}
