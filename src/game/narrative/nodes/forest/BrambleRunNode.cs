using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Bramble Run - A thorny corridor of blackberry brambles.
/// </summary>
public class BrambleRunNode : NarrationNode
{
    public override string NodeId => "bramble_run";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new BoarArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "carefully navigating the brambles";
    public override string TransitionDescription => "push through the brambles";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "thorn", "berry", "bramble", "vine" };
    
    private static readonly string[] Moods = { "thorny", "tangled", "scratching", "productive", "wild", "dense", "prickly", "guarded" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} bramble run";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"navigating a {mood} bramble run";
    }
    
    public sealed class Blackberry : Item
    {
        public override string ItemId => "wild_blackberry";
        public override string DisplayName => "Wild Blackberry";
        public override string Description => "A cluster of ripe blackberries";
        public override List<string> OutcomeKeywords => new() { "berry", "drupe", "cluster", "stain" };
    }
    
    public sealed class BrambleThorn : Item
    {
        public override string ItemId => "bramble_run_thorn";
        public override string DisplayName => "Bramble Thorn";
        public override string Description => "A sharp curved thorn from a bramble cane";
        public override List<string> OutcomeKeywords => new() { "barb", "spine", "hook" };
    }
}
