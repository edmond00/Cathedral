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
    
    public override List<string> NodeKeywords => new() { "lip", "precipice", "ice", "danger" };
    
    private static readonly string[] Moods = { "precipitous", "frozen", "deep", "dangerous" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} ravine lip";
    }
    
    public sealed class RavineQuartz : Item
    {
        public override string ItemId => "frozen_ravine_lip_ravine_quartz";
        public override string DisplayName => "Ravine Quartz";
        public override string Description => "Milky quartz collectible from the ravine edge";
        public override List<string> OutcomeKeywords => new() { "quartz", "vein", "mineral" };
    }
    
    public sealed class FrozenRock : Item
    {
        public override string ItemId => "frozen_ravine_lip_frozen_rock";
        public override string DisplayName => "Frozen Rock";
        public override string Description => "Ice-covered rock at edge";
        public override List<string> OutcomeKeywords => new() { "rock", "ice", "glaze" };
    }
    
    public sealed class IcicleFormation : Item
    {
        public override string ItemId => "frozen_ravine_lip_icicle_formation";
        public override string DisplayName => "Icicle Formation";
        public override string Description => "Cluster of icicles hanging over edge";
        public override List<string> OutcomeKeywords => new() { "icicle", "formation", "crystal" };
    }
}
