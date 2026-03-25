using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class CliffBaseNode : PyramidalFeatureNode
{
    public override int MinAltitude => 2;
    public override int MaxAltitude => 6;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(CliffTopNode);
    
    public override string NodeId => "cliff_base";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new SavageArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "standing at the cliff base";
    public override string TransitionDescription => "approach the cliff base";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "cliff", "stone", "wall", "shadow" };
    
    private static readonly string[] Moods = { "imposing", "shadowed", "towering", "daunting" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} cliff base";
    }
    
    public sealed class LooseRock : Item
    {
        public override string ItemId => "cliff_base_loose_rock";
        public override string DisplayName => "Loose Rock";
        public override string Description => "Unstable rock at the cliff base";
        public override List<string> OutcomeKeywords => new() { "rock", "debris", "rockfall" };
    }
    
    public sealed class CrumblingStone : Item
    {
        public override string ItemId => "cliff_base_crumbling_stone";
        public override string DisplayName => "Crumbling Stone";
        public override string Description => "Weathered stone fragments from erosion";
        public override List<string> OutcomeKeywords => new() { "stone", "fragment", "erosion" };
    }
}
