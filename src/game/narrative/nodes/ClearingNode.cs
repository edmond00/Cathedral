using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes;

/// <summary>
/// A forest clearing - an open space surrounded by trees.
/// </summary>
public class ClearingNode : NarrationNode
{
    public override string NodeId => "clearing";
    public override bool IsEntryNode => true;
    
    // Keywords that describe this node itself (for being discovered as a transition)
    public override List<string> NodeKeywords => new() { "meadow", "glade", "open", "grassy", "sunlit", "bright", "flowers", "birdsong", "quiet", "space" };
    
    public override List<OutcomeBase> PossibleOutcomes => new()
    {
        new StreamNode(),
        new BerryBushNode(),
        new MushroomPatchNode()
    };
    
    private static readonly string[] Moods = { "peaceful", "quiet", "bright", "shadowy", "misty", "sun-dappled", "verdant", "ancient" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} forest clearing";
    }
}
