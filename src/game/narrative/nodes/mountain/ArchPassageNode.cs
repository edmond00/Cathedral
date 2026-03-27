using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class ArchPassageNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 7;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(ArchCrestNode);
    
    public override string NodeId => "arch_passage";
    public override string ContextDescription => "through the rock arch passage";
    public override string TransitionDescription => "pass through the arch passage";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a shadowed rock <arch> framing the way"), KeywordInContext.Parse("a narrow <passage> between stone walls"), KeywordInContext.Parse("a natural rock <gateway> into the mountain"), KeywordInContext.Parse("a dark stone <portal> open to the wind") };
    
    private static readonly string[] Moods = { "shadowed", "framed", "impressive", "sheltered" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} arch passage";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"through a {mood} arch passage";
    }
    
}
