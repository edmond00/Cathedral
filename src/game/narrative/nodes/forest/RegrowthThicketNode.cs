using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Regrowth Thicket - Young vegetation reclaiming disturbed ground.
/// Associated with: Wildwood
/// </summary>
public class RegrowthThicketNode : NarrationNode
{
    public override string NodeId => "regrowth_thicket";
    public override string ContextDescription => "pushing through regrowth";
    public override string TransitionDescription => "enter the regrowth";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a vigorous new <shoot> pushing through soil"), KeywordInContext.Parse("some <pioneer> plants reclaiming disturbed ground"), KeywordInContext.Parse("the dense <regrowth> pressing in on every side") };
    
    private static readonly string[] Moods = { "vigorous", "new", "thriving", "reclaiming", "dense", "fresh", "recovering", "pioneer" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} regrowth thicket";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"pushing through a {mood} regrowth thicket";
    }
    
    public override List<Item> GetItems() => new() { new PioneerSeed(), new FreshShoot() };

    public sealed class PioneerSeed : Item
    {
        public override string ItemId => "pioneer_seed";
        public override string DisplayName => "Pioneer Seeds";
        public override string Description => "Seeds from fast-growing pioneer plants";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a light <propagule> from a pioneer plant"), KeywordInContext.Parse("a ready <germination> in the disturbed earth") };
    }
    
    public sealed class FreshShoot : Item
    {
        public override string ItemId => "regrowth_thicket_fresh_shoot";
        public override string DisplayName => "Fresh Shoot";
        public override string Description => "A vigorous new shoot pushing through soil";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a pale fresh <sprout> at the soil surface"), KeywordInContext.Parse("a remarkable <vigor> in the young plant stems") };
    }
}
