using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Plain;

/// <summary>
/// Grassland — vast flat terrain of tall grass, sparse trees, wide sky.
/// </summary>
public class GrasslandNode : NarrationNode
{
    public override string NodeId => "grassland";
    public override string ContextDescription => "crossing the open grassland";
    public override string TransitionDescription => "move into the grassland";
    public override bool IsEntryNode => true;

    public override List<KeywordInContext> NodeKeywordsInContext => new()
    {
        KeywordInContext.Parse("the tall <grass> swaying in long waves"),
        KeywordInContext.Parse("a broad <sky> pressing down on flat land"),
        KeywordInContext.Parse("some dried <stalks> rattling in the breeze"),
        KeywordInContext.Parse("the faint <trail> of something passing through the grass"),
    };

    private static readonly string[] Moods = { "vast", "flat", "dry", "sweeping", "yellowed", "rustling", "endless", "sparse" };

    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} grassland";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"crossing a {Moods[rng.Next(Moods.Length)]} grassland";
    }
}
