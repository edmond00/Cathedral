using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class FrozenRavineFloorNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 8;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(FrozenRavineLipNode);
    
    public override string NodeId => "frozen_ravine_floor";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "standing on the frozen ravine floor";
    public override string TransitionDescription => "descend to the frozen ravine floor";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the frozen <ravine> floor in permanent shadow"), KeywordInContext.Parse("a clear sheet of <ice> covering the floor"), KeywordInContext.Parse("the complete <confinement> of the ravine walls"), KeywordInContext.Parse("the profound <depth> of the cut ravine") };
    
    private static readonly string[] Moods = { "shadowed", "narrow", "confined", "frigid" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} ravine floor";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} ravine floor";
    }
    
    public sealed class PeakBasalt : Item
    {
        public override string ItemId => "frozen_ravine_floor_peak_basalt";
        public override string DisplayName => "Peak Basalt";
        public override string Description => "Dark volcanic basalt collectible from the ravine floor";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a dark heavy <gabbro> from the ravine floor"), KeywordInContext.Parse("a <volcanic> fragment in the frozen sediment"), KeywordInContext.Parse("an <igneous> basalt block locked in ice") };
    }
    
    public sealed class GlacierSilt : Item
    {
        public override string ItemId => "frozen_ravine_floor_glacier_silt";
        public override string DisplayName => "Glacier Silt";
        public override string Description => "Fine glacial sediment collectible from the floor";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a fine pale <loess> drifted into the ravine"), KeywordInContext.Parse("the grey glacial <flour> coating the floor"), KeywordInContext.Parse("a dark <moraine> deposit frozen in place") };
    }
}
