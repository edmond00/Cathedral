using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class WindPackedDriftCrestNode : PyramidalFeatureNode
{
    public override int MinAltitude => 3;
    public override int MaxAltitude => 6;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(WindPackedDriftHollowNode);
    
    public override string NodeId => "wind_packed_drift_crest";
    public override string ContextDescription => "standing on the wind-packed drift crest";
    public override string TransitionDescription => "climb to the wind-packed drift crest";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the hardened wave-form of the snow <drift> below the feet"), KeywordInContext.Parse("the sharp narrow <crest> of the wind-packed ridge"), KeywordInContext.Parse("the constant <wind> that shaped and packed this form"), KeywordInContext.Parse("the precise <sculpture> the wind has made of the snow") };
    
    private static readonly string[] Moods = { "sculpted", "hardened", "wave-like", "frozen" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} drift crest";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} wind-packed drift crest";
    }
    
}
