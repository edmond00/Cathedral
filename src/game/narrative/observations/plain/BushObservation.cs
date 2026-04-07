using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Narrative.Observations.Plain;

/// <summary>
/// Bush observation — a thorny shrub common across the plain.
/// Items: BushLeaf, Thorn, WildBerry.
/// </summary>
public class BushObservation : ObservationObject
{
    public override string ObservationId => "bush";

    public override List<KeywordInContext> ObservationKeywordsInContext => new()
    {
        KeywordInContext.Parse("a dense <thicket> of tangled thorny branches"),
        KeywordInContext.Parse("some dark <berry>s clustered among the leaves"),
        KeywordInContext.Parse("the sharp <thorn>s catching at passing cloth"),
        KeywordInContext.Parse("a low <mound> of tangled undergrowth"),
    };

    private static readonly string[] Moods = { "thorny", "dense", "tangled", "dark", "overgrown", "low", "wild", "scraggly" };

    public BushObservation()
    {
        SubOutcomes = new List<ConcreteOutcome>
        {
            new BushLeaf(),
            new Thorn(),
            new WildBerry(),
        };
    }

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} bush";
    }
}
