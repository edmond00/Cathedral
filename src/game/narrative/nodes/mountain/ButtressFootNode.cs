using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class ButtressFootNode : PyramidalFeatureNode
{
    public override int MinAltitude => 2;
    public override int MaxAltitude => 5;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(ButtressHeadNode);
    
    public override string NodeId => "buttress_foot";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "at the rock buttress foot";
    public override string TransitionDescription => "descend to the buttress foot";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a massive rock <buttress> anchoring the slope"), KeywordInContext.Parse("the broad <foundation> of ancient stone"), KeywordInContext.Parse("the hard <ground> at the base of the crag"), KeywordInContext.Parse("a sense of <stability> from the solid rock") };
    
    private static readonly string[] Moods = { "massive", "anchored", "solid", "foundational" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} buttress foot";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"at a {mood} buttress foot";
    }
    
    public sealed class RootedShrub : Item
    {
        public override string ItemId => "buttress_foot_rooted_shrub";
        public override string DisplayName => "Rooted Shrub";
        public override string Description => "Hardy plant growing at the rock base";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a thin <tendril> gripping the stone"), KeywordInContext.Parse("a stubborn <tenacity> evident in every root"), KeywordInContext.Parse("a dry <twig> snapping underfoot") };
    }
}
