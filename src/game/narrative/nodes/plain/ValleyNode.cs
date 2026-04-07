using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Plain;

/// <summary>
/// Valley — a shallow depression sheltered from wind, damp ground, lush growth.
/// </summary>
public class ValleyNode : NarrationNode
{
    public override string NodeId => "valley";
    public override string ContextDescription => "descending into the shallow valley";
    public override string TransitionDescription => "descend into the valley";
    public override bool IsEntryNode => true;

    public override List<KeywordInContext> NodeKeywordsInContext => new()
    {
        KeywordInContext.Parse("the sheltered <hollow> cupped between two rises"),
        KeywordInContext.Parse("some damp <earth> soft beneath the grass"),
        KeywordInContext.Parse("a faint <trickle> of water somewhere below"),
        KeywordInContext.Parse("the dense <green> of sheltered growth"),
    };

    private static readonly string[] Moods = { "sheltered", "damp", "quiet", "lush", "shadowed", "still", "secluded", "overgrown" };

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} valley";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"descending into a {Moods[rng.Next(Moods.Length)]} valley";
    }
}
