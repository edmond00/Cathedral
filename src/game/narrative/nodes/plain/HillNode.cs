using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Plain;

/// <summary>
/// Hill — a gentle rise in the plain, offering a wider view, exposed to wind.
/// </summary>
public class HillNode : NarrationNode
{
    public override string NodeId => "hill";
    public override string ContextDescription => "climbing the open hill";
    public override string TransitionDescription => "move up onto the hill";
    public override bool IsEntryNode => true;

    public override List<KeywordInContext> NodeKeywordsInContext => new()
    {
        KeywordInContext.Parse("the gradual <rise> of grassy ground underfoot"),
        KeywordInContext.Parse("a wide <vista> opening to the horizon"),
        KeywordInContext.Parse("the cutting <wind> rolling across the crest"),
        KeywordInContext.Parse("some pale <stone> breaking through the thin turf"),
    };

    private static readonly string[] Moods = { "windswept", "exposed", "grassy", "bare", "lonely", "rolling", "open", "bleak" };

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} hill";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"climbing a {Moods[rng.Next(Moods.Length)]} hill";
    }
}
