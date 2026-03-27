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
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the long swaying <grass> of the clearing"), KeywordInContext.Parse("this small forest <meadow> opening up"), KeywordInContext.Parse("a single <blade> of grass bending in the wind"), KeywordInContext.Parse("a warm <breeze> crossing the open ground") };
    
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
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a tiny <kernel> from the wild grass head"), KeywordInContext.Parse("some dry <chaff> from shaken seed heads") };
    }
    
    public sealed class GrassFlower : Item
    {
        public override string ItemId => "grass_clearing_flower_head";
        public override string DisplayName => "Grass Flower Head";
        public override string Description => "A delicate grass flower panicle";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a delicate <panicle> of grass flowers"), KeywordInContext.Parse("some yellow <pollen> dusting the flower head") };
    }
}
