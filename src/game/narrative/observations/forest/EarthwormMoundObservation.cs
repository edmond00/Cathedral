using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Observations.Forest;

/// <summary>
/// Earthworm Mound observation — rich soil with abundant earthworms.
/// Two sub-outcomes: MoundSoil, WormCasting.
/// </summary>
public class EarthwormMoundObservation : ObservationObject
{
    public override string ObservationId => "earthworm_mound";

    public override List<KeywordInContext> ObservationKeywordsInContext => new()
    {
        KeywordInContext.Parse("a fat earthen <worm> surfacing from below"),
        KeywordInContext.Parse("some dark <casting>s piled on the surface"),
        KeywordInContext.Parse("the rich black <soil> turned by their passage"),
        KeywordInContext.Parse("a network of <tunnel>s just below the mound"),
    };

    private static readonly string[] Moods = { "rich", "fertile", "moist", "active", "living", "productive", "dark", "loamy" };

    public EarthwormMoundObservation()
    {
        SubOutcomes = new List<ConcreteOutcome>
        {
            new MoundSoil(),
            new WormCasting(),
        };
    }

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} earthworm mound";
    }

    public sealed class MoundSoil : Item
    {
        public override string ItemId => "earthworm_rich_soil";
        public override string DisplayName => "Rich Soil";
        public override string Description => "Dark, fertile soil enriched by earthworms";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new()
        {
            KeywordInContext.Parse("some crumbly dark <humus> from the mound"),
            KeywordInContext.Parse("a handful of rich <loam> from around the worms"),
        };
    }

    public sealed class WormCasting : Item
    {
        public override string ItemId => "earthworm_mound_casting";
        public override string DisplayName => "Worm Casting";
        public override string Description => "Small mounds of processed soil left by earthworms";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new()
        {
            KeywordInContext.Parse("a small dark <pellet> of processed soil"),
            KeywordInContext.Parse("some <nutrient>-rich earthworm castings"),
        };
    }
}
