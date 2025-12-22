using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Narrative.Nodes;

/// <summary>
/// A forest stream - flowing water through the forest.
/// </summary>
public class StreamNode : NarrationNode
{
    public override string NodeId => "stream";
    public override bool IsEntryNode => false;
    
    // Keywords that describe this node itself (for being discovered as a transition)
    public override List<string> NodeKeywords => new() { "stream", "brook", "creek" };
    
    public override List<OutcomeBase> PossibleOutcomes => new()
    {
        new Trout(),              // Keywords: fish, water, scales
        new ClearingNode(),       // Keywords: clearing, open, trees (collected from Clearing's outcomes)
        new FeelGoodOutcome()     // Always available
    };
    
    public override List<string> PossibleTransitions => new() { "clearing", "pond" };
    
    private static readonly string[] Moods = { "narrow", "winding", "clear", "rushing", "gentle", "bubbling", "swift", "cold" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} forest stream";
    }
}
