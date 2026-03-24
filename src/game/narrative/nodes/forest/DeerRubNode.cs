using System;
using System.Collections.Generic;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Deer Rub - A tree marked by deer antlers.
/// </summary>
public class DeerRubNode : NarrationNode
{
    public override string NodeId => "deer_rub";

    public override List<NpcEncounterSlot> PossibleEncounters => new()
    {
        new NpcEncounterSlot(new WolfArchetype(), spawnChance: 0.50f),
    };
    public override string ContextDescription => "examining the deer rub";
    public override string TransitionDescription => "investigate the marked tree";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "scraped", "bark", "stripped", "antler", "marks", "territory", "fresh", "scent", "damage", "rubbed" };
    
    private static readonly string[] Moods = { "fresh", "marked", "scraped", "territorial", "recent", "obvious", "stripped", "damaged" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} deer rub";
    }
    
    public sealed class ScrapedBark : Item
    {
        public override string ItemId => "scraped_bark";
        public override string DisplayName => "Scraped Bark";
        public override string Description => "Bark strips torn off by deer antlers";
        public override List<string> OutcomeKeywords => new() { "torn", "fresh", "fibrous", "pale", "bark", "strips", "damaged", "scraped", "shredded", "wood" };
    }
    
    public sealed class DeerScent : Item
    {
        public override string ItemId => "deer_rub_scent_marker";
        public override string DisplayName => "Scent Marker";
        public override string Description => "A patch of bark marked with deer gland secretions";
        public override List<string> OutcomeKeywords => new() { "musky", "territorial", "pungent", "scent", "marker", "chemical", "claim", "strong", "aromatic", "signal" };
    }
}
