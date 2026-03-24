using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class SnowLadenValleyLowerNode : PyramidalFeatureNode
{
    public override int MinAltitude => 8;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(SnowLadenValleyUpperNode);
    
    public override string NodeId => "snow_laden_valley_lower";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "standing in the lower snow-laden valley";
    public override string TransitionDescription => "descend to the lower snow-laden valley";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "valley", "lower", "snow", "laden", "white", "deep", "cold", "broad", "sheltered", "accumulated" };
    
    private static readonly string[] Moods = { "broad", "sheltered", "deep", "quiet" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} lower valley";
    }
    
    public sealed class ValleySchist : Item
    {
        public override string ItemId => "snow_laden_valley_lower_valley_schist";
        public override string DisplayName => "Valley Schist";
        public override string Description => "Foliated metamorphic rock collectible from the valley";
        public override List<string> OutcomeKeywords => new() { "schist", "metamorphic", "foliated", "layered", "flaky", "grey", "mineral", "crystalline", "banded", "collectible" };
    }
    
    public sealed class ValleyMoss : Item
    {
        public override string ItemId => "snow_laden_valley_lower_valley_moss";
        public override string DisplayName => "Valley Moss";
        public override string Description => "Sheltered moss collectible from the lower valley";
        public override List<string> OutcomeKeywords => new() { "moss", "valley", "green", "soft", "damp", "sheltered", "growth", "plant", "cushion", "collectible" };
    }
}
