using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class LowerScreeNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(UpperScreeNode);
    
    public override string NodeId => "lower_scree";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new SavageArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "on the lower scree slope";
    public override string TransitionDescription => "descend to the lower scree";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("some loose <scree> shifting underfoot"), KeywordInContext.Parse("the settled <debris> accumulated at the base"), KeywordInContext.Parse("a bed of coarse <gravel> on the slope"), KeywordInContext.Parse("a thick <deposit> of eroded material") };
    
    private static readonly string[] Moods = { "accumulated", "settled", "deposited", "loose" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} lower scree";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} lower scree";
    }
    
    public override List<Item> GetItems() => new() { new ScreeGravel(), new BuriedRock() };

    public sealed class ScreeGravel : Item
    {
        public override string ItemId => "scree_gravel";
        public override string DisplayName => "Fine Gravel";
        public override string Description => "Small stones settled at the base";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a small <pebble> rattling down the scree"), KeywordInContext.Parse("a flat <stone> settled in the gravel"), KeywordInContext.Parse("a thin <deposit> of angular fragments") };
    }
    
    public sealed class BuriedRock : Item
    {
        public override string ItemId => "lower_scree_buried_rock";
        public override string DisplayName => "Buried Rock";
        public override string Description => "Large stone partially covered by scree";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a large <lithic> block half submerged in scree"), KeywordInContext.Parse("a partial <burial> of stone beneath the slope"), KeywordInContext.Parse("a firm <anchor> point in the shifting debris") };
    }
}
