using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class GullyBottomNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 8;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(GullyLipNode);
    
    public override string NodeId => "gully_bottom";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.40f),
        new NpcEncounterSlot(new BearArchetype(), spawnChance: 0.30f),
    };
    public override string ContextDescription => "in the shaded gully bottom";
    public override string TransitionDescription => "descend to the gully bottom";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a thin <runnel> of water threading the gully"), KeywordInContext.Parse("a thick pad of <moss> on every stone"), KeywordInContext.Parse("the cool <shade> never leaving the bottom"), KeywordInContext.Parse("the deep <dampness> clinging to the walls") };
    
    private static readonly string[] Moods = { "shadowed", "damp", "enclosed", "dark" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} gully bottom";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"in a {mood} gully bottom";
    }
    
    public sealed class GullyMoss : Item
    {
        public override string ItemId => "gully_moss";
        public override string DisplayName => "Thick Moss";
        public override string Description => "Dense mossy carpet in the shade";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a dense <carpet> of moss covering the floor"), KeywordInContext.Parse("the heavy <dampness> seeping through the gully"), KeywordInContext.Parse("a soft <cushion> of green growth underfoot") };
    }
    
}
