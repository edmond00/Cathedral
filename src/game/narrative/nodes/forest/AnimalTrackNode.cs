using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Animal Track - A transversal feature following wildlife paths.
/// </summary>
public class AnimalTrackNode : NarrationNode
{
    public override string NodeId => "animal_track";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.50f),
        new NpcEncounterSlot(new BoarArchetype(), spawnChance: 0.35f),
    };
    public override string ContextDescription => "following the animal track";
    public override string TransitionDescription => "follow the track";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a worn animal <trail> through the undergrowth"), KeywordInContext.Parse("some fresh <droppings> on the path"), KeywordInContext.Parse("a sharp musky <scent> in the air"), KeywordInContext.Parse("these cloven <prints> pressed into the mud") };
    
    private static readonly string[] Moods = { "well-worn", "faint", "fresh", "winding", "meandering", "hidden", "obvious", "subtle" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} animal track";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"following a {mood} animal track";
    }
    
    public sealed class AnimalDroppings : Item
    {
        public override string ItemId => "animal_droppings";
        public override string DisplayName => "Animal Droppings";
        public override string Description => "Fresh droppings indicating recent animal passage";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some dark <pellets> clustered on a stone"), KeywordInContext.Parse("a strong animal <musk> clinging to the ground"), KeywordInContext.Parse("some fresh <scat> still warm to the touch") };
    }
    
    public sealed class TuftOfFur : Item
    {
        public override string ItemId => "animal_track_tuft_of_fur";
        public override string DisplayName => "Tuft of Fur";
        public override string Description => "Animal fur snagged on nearby brush";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some coarse <fiber> snagged on a thorn"), KeywordInContext.Parse("a scrap of rough <pelt> caught on a branch"), KeywordInContext.Parse("a single wiry <filament> of animal hair") };
    }
}
