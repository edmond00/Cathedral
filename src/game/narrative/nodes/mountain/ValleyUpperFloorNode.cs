using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class ValleyUpperFloorNode : PyramidalFeatureNode
{
    public override int MinAltitude => 8;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(ValleyLowerFloorNode);
    
    public override string NodeId => "upper_valley_floor";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "on the wide valley upper floor";
    public override string TransitionDescription => "enter the valley upper floor";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the wide <valley> floor above the lower reaches"), KeywordInContext.Parse("a wide <meadow> of alpine grass"), KeywordInContext.Parse("the tall <grass> swaying across the valley"), KeywordInContext.Parse("the spacious <openness> of the upper floor") };
    
    private static readonly string[] Moods = { "wide", "open", "spacious", "peaceful" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} valley upper floor";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} valley upper floor";
    }
    
    public override List<Item> GetItems() => new() { new MeadowGrass() };

    public sealed class MeadowGrass : Item
    {
        public override string ItemId => "valley_upper_floor_meadow_grass";
        public override string DisplayName => "Meadow Grass";
        public override string Description => "Tall grass covering the valley floor";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a long grass <blade> wet with morning dew"), KeywordInContext.Parse("the rich <abundance> of the sheltered meadow"), KeywordInContext.Parse("the deep <verdure> of the valley grass") };
    }
    
}
