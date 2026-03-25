using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class GlacierTongueLowerNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(GlacierTongueUpperNode);
    
    public override string NodeId => "glacier_tongue_lower";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "standing on the lower ice flow";
    public override string TransitionDescription => "descend to the lower ice flow";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "glacier", "terminus", "ice", "melting" };
    
    private static readonly string[] Moods = { "terminating", "melting", "ending", "transitional" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} lower ice flow";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} lower ice flow";
    }
    
    public sealed class MoraineDirt : Item
    {
        public override string ItemId => "glacier_tongue_lower_moraine_dirt";
        public override string DisplayName => "Moraine Dirt";
        public override string Description => "Dirt and rock from glacier edge";
        public override List<string> OutcomeKeywords => new() { "moraine", "dirt", "debris" };
    }
    
    public sealed class GlacialErratic : Item
    {
        public override string ItemId => "glacier_tongue_lower_glacial_erratic";
        public override string DisplayName => "Glacial Erratic";
        public override string Description => "Glacier-deposited boulder fragment collectible from ice edge";
        public override List<string> OutcomeKeywords => new() { "erratic", "boulder", "glacier" };
    }
}
