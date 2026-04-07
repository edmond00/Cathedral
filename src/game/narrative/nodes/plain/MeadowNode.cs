using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Plain;

/// <summary>
/// Meadow — an open grassy expanse dotted with wildflowers, gentle and exposed.
/// </summary>
public class MeadowNode : NarrationNode
{
    public override string NodeId => "meadow";
    public override string ContextDescription => "wandering through the open meadow";
    public override string TransitionDescription => "move into the open meadow";
    public override bool IsEntryNode => true;

    public override List<KeywordInContext> NodeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a wide <expanse> of unbroken grass ahead"),
        KeywordInContext.Parse("some bright <wildflower>s nodding in the breeze"),
        KeywordInContext.Parse("the warm <stillness> of open air"),
        KeywordInContext.Parse("a gentle <slope> falling away to the south"),
    };

    private static readonly string[] Moods = { "sunlit", "breezy", "quiet", "open", "peaceful", "windswept", "golden", "wide" };

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} meadow";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"wandering through a {Moods[rng.Next(Moods.Length)]} meadow";
    }
}
