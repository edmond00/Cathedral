using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class RavineFloorNode : PyramidalFeatureNode
{
    public override int MinAltitude => 4;
    public override int MaxAltitude => 8;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(RavineRimNode);
    
    public override string NodeId => "ravine_floor";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new BearArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "on the narrow ravine floor";
    public override string TransitionDescription => "descend to the ravine floor";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the narrow <ravine> closing in on both sides"), KeywordInContext.Parse("the flat <floor> of the cut ravine"), KeywordInContext.Parse("the total <confinement> of the gorge walls"), KeywordInContext.Parse("the deep <shadow> that never leaves the bottom") };
    
    private static readonly string[] Moods = { "shadowed", "narrow", "enclosed", "claustrophobic" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} ravine floor";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} ravine floor";
    }
    
    public sealed class WedgedLog : Item
    {
        public override string ItemId => "ravine_floor_wedged_log";
        public override string DisplayName => "Wedged Log";
        public override string Description => "Dead tree trapped between walls";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the rough <bark> of the wedged log"), KeywordInContext.Parse("the pale <wood> exposed where bark split"), KeywordInContext.Parse("the clear <grain> running through the timber") };
    }
}
