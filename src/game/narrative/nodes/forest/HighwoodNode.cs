using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Highwood - Level 4. Tall-trunked forest with sparse understory.
/// </summary>
public class HighwoodNode : NarrationNode
{
    public override string NodeId => "highwood";
    public override string ContextDescription => "walking beneath the highwood";
    public override string TransitionDescription => "enter the highwood";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("some pale <lichen> crusted on the upper bark"), KeywordInContext.Parse("the rough furrowed <bark> of a tall trunk"), KeywordInContext.Parse("the wide spreading <roots> at the trunk base"), KeywordInContext.Parse("the dizzying <height> of the trunks overhead") };
    
    private static readonly string[] Moods = { "towering", "majestic", "imposing", "grand", "austere", "noble", "dominant", "lofty" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} highwood";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"walking beneath a {mood} highwood";
    }
    
    public override List<Item> GetItems() => new() { new HighwoodLichen(), new BarkShavings() };

    public sealed class HighwoodLichen : Item
    {
        public override string ItemId => "highwood_lichen";
        public override string DisplayName => "Highwood Lichen";
        public override string Description => "Pale lichen scraped from highwood bark";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the flat <crustose> lichen on the bark"), KeywordInContext.Parse("a pale <crust> scraped from the highwood trunk") };
    }
    
    public sealed class BarkShavings : Item
    {
        public override string ItemId => "highwood_bark_shavings";
        public override string DisplayName => "Bark Shavings";
        public override string Description => "Thin strips of bark peeled from tall trunks";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the outer <cortex> peeled in thin strips"), KeywordInContext.Parse("some dry bark <tinder> easily catching a spark") };
    }
}
