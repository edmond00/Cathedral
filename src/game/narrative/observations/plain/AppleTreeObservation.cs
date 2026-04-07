using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Narrative.Observations.Plain;

/// <summary>
/// Apple Tree observation — a solitary apple tree, gnarled and laden with fruit or stripped bare.
/// Items: AppleLeaf, Branch, Apple, Bark.
/// </summary>
public class AppleTreeObservation : ObservationObject
{
    public override string ObservationId => "apple_tree";

    public override List<KeywordInContext> ObservationKeywordsInContext => new()
    {
        KeywordInContext.Parse("a gnarled <apple> tree standing alone in the grass"),
        KeywordInContext.Parse("the wide <canopy> of a solitary fruit tree"),
        KeywordInContext.Parse("some heavy <bough>s drooping with fruit"),
        KeywordInContext.Parse("the sweet <scent> of ripe apples on the air"),
    };

    private static readonly string[] Moods = { "gnarled", "laden", "solitary", "weathered", "ancient", "spreading", "crooked", "still" };

    public AppleTreeObservation()
    {
        SubOutcomes = new List<ConcreteOutcome>
        {
            new AppleLeaf(),
            new Branch(),
            new Apple(),
            new Bark(),
        };
    }

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} apple tree";
    }
}
