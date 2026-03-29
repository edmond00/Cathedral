using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Observations.Forest;

/// <summary>
/// Snail Trail observation — a glistening path left by forest snails.
/// Three sub-outcomes: SnailShell, SlimeTrail, MossClump.
/// </summary>
public class SnailTrailObservation : ObservationObject
{
    public override string ObservationId => "snail_trail";

    public override List<KeywordInContext> ObservationKeywordsInContext => new()
    {
        KeywordInContext.Parse("the glistening <slime> trail on a leaf"),
        KeywordInContext.Parse("the winding <trail> left across the bark"),
        KeywordInContext.Parse("the perfect <spiral> of an empty shell"),
        KeywordInContext.Parse("the iridescent <mucus> still wet underfoot"),
    };

    private static readonly string[] Moods = { "glistening", "silvery", "shimmering", "fresh", "wet", "meandering", "slow", "deliberate" };

    public SnailTrailObservation()
    {
        SubOutcomes = new List<ConcreteOutcome>
        {
            new SnailShell(),
            new SlimeTrail(),
            new MossClump(),
        };
    }

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} snail trail";
    }

    public sealed class SnailShell : Item
    {
        public override string ItemId => "empty_snail_shell";
        public override string DisplayName => "Empty Snail Shell";
        public override string Description => "A spiral snail shell, the occupant long gone";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new()
        {
            KeywordInContext.Parse("the final <whorl> of a spiral snail shell"),
            KeywordInContext.Parse("the pure white <calcium> of the empty shell"),
        };
    }

    public sealed class SlimeTrail : Item
    {
        public override string ItemId => "snail_trail_slime";
        public override string DisplayName => "Fresh Slime Trail";
        public override string Description => "Glistening mucus trail still wet";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new()
        {
            KeywordInContext.Parse("the sticky <mucus> left by the passing snail"),
            KeywordInContext.Parse("a faint <iridescence> in the slime trail"),
        };
    }

    public sealed class MossClump : Item
    {
        public override string ItemId => "snail_trail_moss_clump";
        public override string DisplayName => "Damp Moss Clump";
        public override string Description => "Small clump of moss where snails graze and rest";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new()
        {
            KeywordInContext.Parse("a damp <bryophyte> tuft where the snails graze"),
            KeywordInContext.Parse("a soft green <tuft> of moss with trails across it"),
        };
    }
}
