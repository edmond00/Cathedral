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
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("an imposing <cliff> face looming above"), KeywordInContext.Parse("a grey <stone> slick with moisture"), KeywordInContext.Parse("the sheer rock <wall> blocking the sky"), KeywordInContext.Parse("a deep <shadow> pooled at the base") };
    
    private static readonly string[] Moods = { "imposing", "shadowed", "towering", "daunting" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} cliff base";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing at a {mood} cliff base";
    }
    
    public override List<Item> GetItems() => new() { new LooseRock(), new CrumblingStone() };

    public sealed class LooseRock : Item
    {
        public override string ItemId => "cliff_base_loose_rock";
        public override string DisplayName => "Loose Rock";
        public override string Description => "Unstable rock at the cliff base";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some loose <scree> shifting at the base"), KeywordInContext.Parse("a pile of <debris> freshly fallen"), KeywordInContext.Parse("the scar of an old <rockfall> on the cliff") };
    }
    
    public sealed class CrumblingStone : Item
    {
        public override string ItemId => "cliff_base_crumbling_stone";
        public override string DisplayName => "Crumbling Stone";
        public override string Description => "Weathered stone fragments from erosion";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a heap of coarse <rubble> at the cliff foot"), KeywordInContext.Parse("a small <fragment> broken from the face"), KeywordInContext.Parse("the slow work of <erosion> on the stone") };
    }
}
