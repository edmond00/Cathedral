using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Worn Ground Path - A transversal feature of a well-trodden trail.
/// </summary>
public class WornGroundPathNode : NarrationNode
{
    public override string NodeId => "worn_ground_path";
    public override string ContextDescription => "following the worn path";
    public override string TransitionDescription => "take the worn path";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "trampled", "bare", "earth", "compacted", "trail", "smooth", "travelled", "packed", "dusty", "clear" };
    
    private static readonly string[] Moods = { "well-worn", "trampled", "smooth", "clear", "obvious", "easy", "traveled", "beaten" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} worn ground path";
    }
}
