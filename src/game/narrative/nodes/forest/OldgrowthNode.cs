using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Oldgrowth - Level 9. Ancient forest with massive trees and decay.
/// </summary>
public class OldgrowthNode : NarrationNode
{
    public override string NodeId => "oldgrowth";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new BoarArchetype(), spawnChance: 0.25f),
        new NpcEncounterSlot(new DruidArchetype(), spawnChance: 0.15f),
    };
    public override string ContextDescription => "exploring the oldgrowth";
    public override string TransitionDescription => "enter the oldgrowth";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "ancient", "giant", "dead", "decaying", "layered", "humus", "massive", "snag", "old", "timeless" };
    
    private static readonly string[] Moods = { "ancient", "primordial", "timeless", "venerable", "aged", "eternal", "prehistoric", "primeval" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} oldgrowth";
    }
    
    public sealed class AncientWood : Item
    {
        public override string ItemId => "oldgrowth_ancient_wood";
        public override string DisplayName => "Ancient Wood";
        public override string Description => "Weathered wood from ancient oldgrowth trees";
        public override List<string> OutcomeKeywords => new() { "wood", "ancient", "weathered", "gray", "solid", "old", "hard", "timeless", "dense", "preserved" };
    }
    
    public sealed class OldgrowthResin : Item
    {
        public override string ItemId => "oldgrowth_oldgrowth_resin";
        public override string DisplayName => "Oldgrowth Resin";
        public override string Description => "Hardened resin oozing from ancient trees";
        public override List<string> OutcomeKeywords => new() { "resin", "hardened", "amber", "ancient", "sticky", "tree", "golden", "preserved", "crystalline", "sap" };
    }
}
