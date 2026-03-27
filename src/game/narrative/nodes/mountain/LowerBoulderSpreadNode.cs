using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class LowerBoulderSpreadNode : PyramidalFeatureNode
{
    public override int MinAltitude => 4;
    public override int MaxAltitude => 8;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(UpperBoulderSpreadNode);
    
    public override string NodeId => "lower_boulder_spread";
    public override string ContextDescription => "in the lower boulder spread";
    public override string TransitionDescription => "descend into the lower boulder spread";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a moss-covered <boulder> blocking the path"), KeywordInContext.Parse("a thick pad of <moss> on every surface"), KeywordInContext.Parse("the permanent <shade> between the stones"), KeywordInContext.Parse("the pervasive <dampness> under the boulders") };
    
    private static readonly string[] Moods = { "settled", "mossy", "shaded", "ancient" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} lower boulder spread";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"in a {mood} lower boulder spread";
    }
    
}
