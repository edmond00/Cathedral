using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class SnowCorniceCrestNode : PyramidalFeatureNode
{
    public override int MinAltitude => 0;
    public override int MaxAltitude => 2;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(SnowCorniceFallLineNode);
    
    public override string NodeId => "snow_cornice_crest";
    public override string ContextDescription => "standing on the snow cornice crest";
    public override string TransitionDescription => "approach the snow cornice crest";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the curling lip of the <cornice> projecting into space"), KeywordInContext.Parse("the hollow void beneath the snow <overhang>"), KeywordInContext.Parse("the dense wind-packed <snow> that forms this precarious crest"), KeywordInContext.Parse("a sense of <danger> from the invisible fracture line") };
    
    private static readonly string[] Moods = { "precarious", "overhanging", "delicate", "dangerous" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} snow cornice crest";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} snow cornice crest";
    }
    
}
