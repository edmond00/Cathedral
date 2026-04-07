using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Narrative.Observations.Plain;

/// <summary>
/// Flower Patch observation — a colourful patch of wildflowers.
/// Items: Daisy, Poppy, Clover, Dandelion.
/// </summary>
public class FlowerPatchObservation : ObservationObject
{
    public override string ObservationId => "flower_patch";

    public override List<KeywordInContext> ObservationKeywordsInContext => new()
    {
        KeywordInContext.Parse("a bright <patch> of colour in the grass"),
        KeywordInContext.Parse("the faint sweet <fragrance> drifting from the flowers"),
        KeywordInContext.Parse("some nodding <daisy> heads catching the light"),
        KeywordInContext.Parse("a <poppy> burning red against the green"),
    };

    private static readonly string[] Moods = { "bright", "fragrant", "colourful", "quiet", "cheerful", "scattered", "wild", "vivid" };

    public FlowerPatchObservation()
    {
        SubOutcomes = new List<ConcreteOutcome>
        {
            new Daisy(),
            new Poppy(),
            new Clover(),
            new Dandelion(),
        };
    }

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} flower patch";
    }
}
