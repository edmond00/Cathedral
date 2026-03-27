using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class WallTopNode : PyramidalFeatureNode
{
    public override int MinAltitude => 6;
    public override int MaxAltitude => 9;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(WallBaseNode);
    
    public override string NodeId => "wall_top";
    public override string ContextDescription => "at the lower cliff wall top";
    public override string TransitionDescription => "climb to the wall top";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the top of the rock <wall> open to the wind"), KeywordInContext.Parse("the sheer <precipice> falling below"), KeywordInContext.Parse("a breathtaking <overlook> from the cliff top"), KeywordInContext.Parse("the vertiginous <drop> at the wall edge") };
    
    private static readonly string[] Moods = { "exposed", "vertical", "precipitous", "commanding" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} wall top";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"at a {mood} wall top";
    }
    
}
