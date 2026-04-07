using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Narrative.Observations.Plain;

/// <summary>
/// Boulder observation — a large stone half-buried in the plain ground.
/// Items: Rock, Moss, Mushroom.
/// </summary>
public class BoulderObservation : ObservationObject
{
    public override string ObservationId => "boulder";

    public override List<KeywordInContext> ObservationKeywordsInContext => new()
    {
        KeywordInContext.Parse("a great grey <boulder> rising from the turf"),
        KeywordInContext.Parse("the damp <moss> carpeting the north face of the stone"),
        KeywordInContext.Parse("the cold smooth <surface> of old weathered rock"),
        KeywordInContext.Parse("a <shadow> pooled at the base of the stone"),
    };

    private static readonly string[] Moods = { "grey", "weathered", "mossy", "cold", "ancient", "massive", "silent", "half-buried" };

    public BoulderObservation()
    {
        SubOutcomes = new List<ConcreteOutcome>
        {
            new Rock(),
            new Moss(),
            new Mushroom(),
        };
    }

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} boulder";
    }
}
