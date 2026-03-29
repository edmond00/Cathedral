using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Observations.Forest;

/// <summary>
/// Squirrel Cache observation — a hidden stash of nuts and seeds.
/// Two sub-outcomes: CachedNuts, SquirrelFur.
/// </summary>
public class SquirrelCacheObservation : ObservationObject
{
    public override string ObservationId => "squirrel_cache";

    public override List<KeywordInContext> ObservationKeywordsInContext => new()
    {
        KeywordInContext.Parse("a hidden <cache> of nuts in a hollow"),
        KeywordInContext.Parse("some brown <acorn>s buried just below the surface"),
        KeywordInContext.Parse("a mixed <hoard> of seeds and nuts found here"),
    };

    private static readonly string[] Moods = { "hidden", "secretive", "buried", "hoarded", "stashed", "concealed", "protected", "stored" };

    public SquirrelCacheObservation()
    {
        SubOutcomes = new List<ConcreteOutcome>
        {
            new CachedNuts(),
            new SquirrelFur(),
        };
    }

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} squirrel cache";
    }

    public sealed class CachedNuts : Item
    {
        public override string ItemId => "cached_nuts";
        public override string DisplayName => "Cached Nuts";
        public override string Description => "A handful of nuts from a squirrel's stash";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new()
        {
            KeywordInContext.Parse("some hard-shelled <caryopsis> nuts from the stash"),
            KeywordInContext.Parse("a cracked <shell> from a previously eaten nut"),
        };
    }

    public sealed class SquirrelFur : Item
    {
        public override string ItemId => "squirrel_cache_fur";
        public override string DisplayName => "Squirrel Fur Tuft";
        public override string Description => "A tuft of grey squirrel fur left at the cache";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new()
        {
            KeywordInContext.Parse("a tuft of grey <pelage> from a squirrel's flank"),
            KeywordInContext.Parse("some bushy <tail> hairs caught on the bark"),
        };
    }
}
