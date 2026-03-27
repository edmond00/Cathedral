using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class FrozenRavineLipNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 8;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(FrozenRavineFloorNode);
    
    public override string NodeId => "frozen_ravine_lip";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "standing at the frozen ravine lip";
    public override string TransitionDescription => "approach the frozen ravine lip";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the icy <lip> of the frozen ravine"), KeywordInContext.Parse("the sheer <precipice> dropping into the ravine"), KeywordInContext.Parse("a clear sheet of <ice> cracking at the edge"), KeywordInContext.Parse("the mortal <danger> of the ravine lip") };
    
    private static readonly string[] Moods = { "precipitous", "frozen", "deep", "dangerous" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} ravine lip";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing at a {mood} ravine lip";
    }
    
    public sealed class RavineQuartz : Item
    {
        public override string ItemId => "frozen_ravine_lip_ravine_quartz";
        public override string DisplayName => "Ravine Quartz";
        public override string Description => "Milky quartz collectible from the ravine edge";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a pale <silica> vein cutting the ravine wall"), KeywordInContext.Parse("a white quartz <vein> in the frozen rock"), KeywordInContext.Parse("a bright <mineral> streak in the ravine stone") };
    }
    
    public sealed class FrozenRock : Item
    {
        public override string ItemId => "frozen_ravine_lip_frozen_rock";
        public override string DisplayName => "Frozen Rock";
        public override string Description => "Ice-covered rock at edge";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a smooth <cobble> locked in the ice"), KeywordInContext.Parse("a thick sheet of <ice> coating the rock"), KeywordInContext.Parse("a thin <glaze> of ice over the surface") };
    }
    
    public sealed class IcicleFormation : Item
    {
        public override string ItemId => "frozen_ravine_lip_icicle_formation";
        public override string DisplayName => "Icicle Formation";
        public override string Description => "Cluster of icicles hanging over edge";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a tapering ice <pendant> above the drop"), KeywordInContext.Parse("a frozen <speleothem> dripping from the edge"), KeywordInContext.Parse("a clear ice <crystal> growing from the rock") };
    }
}
