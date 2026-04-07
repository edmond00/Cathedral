using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Narrative.Observations.Plain;

/// <summary>
/// Pine Tree observation — a lone pine standing at the edge of the plain.
/// Items: Branch, Bark, PineSap, PineCone, PineNeedle.
/// </summary>
public class PineTreeObservation : ObservationObject
{
    public override string ObservationId => "pine_tree";

    public override List<KeywordInContext> ObservationKeywordsInContext => new()
    {
        KeywordInContext.Parse("a tall <pine> rising above the surrounding grass"),
        KeywordInContext.Parse("the sharp <resin> smell of pine on the air"),
        KeywordInContext.Parse("a carpet of brown <needle>s around the base"),
        KeywordInContext.Parse("some heavy <cone>s clustered on the lower branches"),
    };

    private static readonly string[] Moods = { "tall", "solitary", "resinous", "dark", "wind-bent", "dense", "towering", "scraggly" };

    public PineTreeObservation()
    {
        SubOutcomes = new List<ConcreteOutcome>
        {
            new Branch(),
            new Bark(),
            new PineSap(),
            new PineCone(),
            new PineNeedle(),
        };
    }

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} pine tree";
    }
}
