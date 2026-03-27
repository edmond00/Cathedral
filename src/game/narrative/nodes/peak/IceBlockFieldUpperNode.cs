using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class IceBlockFieldUpperNode : PyramidalFeatureNode
{
    public override int MinAltitude => 6;
    public override int MaxAltitude => 9;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(IceBlockFieldLowerNode);
    
    public override string NodeId => "upper_ice_block_field";
    public override string ContextDescription => "standing in the upper ice block field";
    public override string TransitionDescription => "climb through the upper ice blocks";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a clear sheet of <ice> between the blocks"), KeywordInContext.Parse("a massive ice <block> fractured from above"), KeywordInContext.Parse("the confusing <maze> of upper ice seracs"), KeywordInContext.Parse("the total <chaos> of the fractured icefall") };
    
    private static readonly string[] Moods = { "chaotic", "fractured", "jumbled", "maze-like" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} upper ice blocks";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing in a {mood} upper ice block field";
    }
    
}
