using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Observations.Forest;

/// <summary>
/// Ant Colony observation — a busy anthill with foraging ants.
/// Two sub-outcomes: AntEggs, ForamicAcid.
/// </summary>
public class AntColonyObservation : ObservationObject
{
    public override string ObservationId => "ant_colony";

    public override List<KeywordInContext> ObservationKeywordsInContext => new()
    {
        KeywordInContext.Parse("a heaped pine-needle <mound> at the base"),
        KeywordInContext.Parse("some small <workers> hauling fragments across the ground"),
        KeywordInContext.Parse("those foraging <trails> radiating outward"),
        KeywordInContext.Parse("a deep underground <chamber> beneath the surface"),
    };

    private static readonly string[] Moods = { "busy", "industrious", "swarming", "organized", "active", "thriving", "teeming", "bustling" };

    public AntColonyObservation()
    {
        SubOutcomes = new List<ConcreteOutcome>
        {
            new AntEggs(),
            new ForamicAcid(),
        };
    }

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} ant colony";
    }

    public sealed class AntEggs : Item
    {
        public override string ItemId => "ant_colony_ant_eggs";
        public override string DisplayName => "Ant Eggs";
        public override string Description => "Tiny pale eggs from the ant colony";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new()
        {
            KeywordInContext.Parse("some pale <pupae> wrapped in silk"),
            KeywordInContext.Parse("some wriggling <larvae> in the nest chamber"),
            KeywordInContext.Parse("the soft white <brood> clustered together"),
        };
    }

    public sealed class ForamicAcid : Item
    {
        public override string ItemId => "ant_colony_foramic_acid";
        public override string DisplayName => "Foramic Acid";
        public override string Description => "Pungent defensive secretion from worker ants";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new()
        {
            KeywordInContext.Parse("a sharp <formate> smell stinging the nostrils"),
            KeywordInContext.Parse("a biting <secretion> sprayed by the workers"),
            KeywordInContext.Parse("an instinctive <defense> rising from the colony"),
        };
    }
}
