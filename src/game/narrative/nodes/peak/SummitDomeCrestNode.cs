using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class SummitDomeCrestNode : PyramidalFeatureNode
{
    public override int MinAltitude => 0;
    public override int MaxAltitude => 1;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(SummitDomeShoulderNode);
    
    public override string NodeId => "summit_dome_crest";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new SavageArchetype(), spawnChance: 0.20f),
    };
    public override string ContextDescription => "standing atop the summit dome crest";
    public override string TransitionDescription => "ascend to the summit dome crest";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the highest <summit> dome above everything"), KeywordInContext.Parse("the wind-polished <crest> of the summit dome"), KeywordInContext.Parse("the vast open <sky> pressing from all sides"), KeywordInContext.Parse("the total <exposure> of the dome top") };
    
    private static readonly string[] Moods = { "windswept", "exposed", "majestic", "austere" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} summit dome crest";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing atop a {mood} summit dome crest";
    }
    
    public sealed class FrozenCrystal : Item
    {
        public override string ItemId => "summit_dome_crest_frozen_crystal";
        public override string DisplayName => "Frozen Crystal";
        public override string Description => "Ice crystal formed by extreme altitude";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a branching <dendrite> of ice on the summit rock"), KeywordInContext.Parse("a clear sheet of <ice> on the dome crest"), KeywordInContext.Parse("the extreme <altitude> felt in every breath") };
    }
    
    public sealed class SummitPolishedStone : Item
    {
        public override string ItemId => "summit_polished_stone";
        public override string DisplayName => "Summit-Polished Stone";
        public override string Description => "Stone smoothed by endless winds";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a smooth <cobble> shaped by summit winds"), KeywordInContext.Parse("the relentless <wind> polishing the stone"), KeywordInContext.Parse("the sense of <endurance> in the ancient rock") };
    }
    
    public sealed class SummitGranite : Item
    {
        public override string ItemId => "summit_dome_crest_summit_granite";
        public override string DisplayName => "Summit Granite";
        public override string Description => "Peak granite collectible from the highest point";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a coarse <pegmatite> crystal at the summit"), KeywordInContext.Parse("a dense <igneous> block from the dome core"), KeywordInContext.Parse("the absolute <apex> of the summit granite") };
    }
}
