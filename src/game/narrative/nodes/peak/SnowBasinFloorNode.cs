using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class SnowBasinFloorNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 8;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(SnowBasinRimNode);
    
    public override string NodeId => "snow_basin_floor";
    public override string ContextDescription => "standing on the snow basin floor";
    public override string TransitionDescription => "descend to the snow basin floor";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the flat floor of the enclosed <basin> all around"), KeywordInContext.Parse("the deep <bowl> shape of the cirque holding snow and silence"), KeywordInContext.Parse("the undisturbed <snow> lying thick across the basin floor"), KeywordInContext.Parse("the <hollow> of the basin collecting cold air at night") };
    
    private static readonly string[] Moods = { "sheltered", "quiet", "deep", "enclosed" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} basin floor";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} snow basin floor";
    }
    
}
