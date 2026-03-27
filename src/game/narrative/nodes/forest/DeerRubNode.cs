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
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("some <antler> gouges cut deep into the bark"), KeywordInContext.Parse("the stripped <bark> hanging in shreds below"), KeywordInContext.Parse("a strong <scent> left as a territorial mark") };
    
    private static readonly string[] Moods = { "fresh", "marked", "scraped", "territorial", "recent", "obvious", "stripped", "damaged" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} deer rub";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} deer rub";
    }
    
    public sealed class ScrapedBark : Item
    {
        public override string ItemId => "scraped_bark";
        public override string DisplayName => "Scraped Bark";
        public override string Description => "Bark strips torn off by deer antlers";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a curled <shaving> of torn bark"), KeywordInContext.Parse("a pale <strip> of inner wood exposed by the rubbing") };
    }
    
}
