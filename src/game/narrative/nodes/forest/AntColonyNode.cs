using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Ant Colony - A busy anthill with foraging ants.
/// </summary>
public class AntColonyNode : NarrationNode
{
    public override string NodeId => "ant_colony";
    public override string ContextDescription => "watching the ant colony";
    public override string TransitionDescription => "observe the ants";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a heaped pine-needle <mound> at the base"), KeywordInContext.Parse("some small <workers> hauling fragments across the ground"), KeywordInContext.Parse("those foraging <trails> radiating outward"), KeywordInContext.Parse("a deep underground <chamber> beneath the surface") };
    
    private static readonly string[] Moods = { "busy", "industrious", "swarming", "organized", "active", "thriving", "teeming", "bustling" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} ant colony";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"watching a {mood} ant colony";
    }
    
    public sealed class AntEggs : Item
    {
        public override string ItemId => "ant_colony_ant_eggs";
        public override string DisplayName => "Ant Eggs";
        public override string Description => "Tiny pale eggs from the ant colony";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some pale <pupae> wrapped in silk"), KeywordInContext.Parse("some wriggling <larvae> in the nest chamber"), KeywordInContext.Parse("the soft white <brood> clustered together") };
    }
    
    public sealed class ForamicAcid : Item
    {
        public override string ItemId => "ant_colony_foramic_acid";
        public override string DisplayName => "Foramic Acid";
        public override string Description => "Pungent defensive secretion from worker ants";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a sharp <formate> smell stinging the nostrils"), KeywordInContext.Parse("a biting <secretion> sprayed by the workers"), KeywordInContext.Parse("an instinctive <defense> rising from the colony") };
    }
}
