using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class ValleyLowerFloorNode : PyramidalFeatureNode
{
    public override int MinAltitude => 8;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(ValleyUpperFloorNode);
    
    public override string NodeId => "lower_valley_floor";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new BoarArchetype(), spawnChance: 0.25f),
        new NpcEncounterSlot(new HermitArchetype(), spawnChance: 0.10f),
    };
    public override string ContextDescription => "on the wide valley lower floor";
    public override string TransitionDescription => "descend to the valley lower floor";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "valley", "floor", "fertility", "shelter" };
    
    private static readonly string[] Moods = { "fertile", "lush", "peaceful", "sheltered" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} valley lower floor";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} valley lower floor";
    }
    
    public sealed class ValleySoil : Item
    {
        public override string ItemId => "valley_soil";
        public override string DisplayName => "Rich Soil";
        public override string Description => "Dark fertile earth in the valley";
        public override List<string> OutcomeKeywords => new() { "humus", "earth", "fertility" };
    }
    
}
