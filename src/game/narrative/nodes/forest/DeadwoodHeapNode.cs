using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Deadwood Heap - A collapsed pile of dead branches and trunks.
/// Associated with: Blackwood
/// </summary>
public class DeadwoodHeapNode : NarrationNode
{
    public override string NodeId => "deadwood_heap";
    public override string ContextDescription => "climbing over deadwood";
    public override string TransitionDescription => "climb the deadwood heap";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "collapsed", "dead", "pile", "branches", "decay", "tangle", "heap", "brittle", "grey", "stacked" };
    
    private static readonly string[] Moods = { "collapsed", "tangled", "dead", "brittle", "chaotic", "decaying", "piled", "grey" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} deadwood heap";
    }
    
    public sealed class DeadBranch : Item
    {
        public override string ItemId => "dead_branch";
        public override string DisplayName => "Dead Branch";
        public override string Description => "A brittle, lifeless branch from the heap";
        public override List<string> OutcomeKeywords => new() { "brittle", "grey", "dead", "dry", "branch", "lifeless", "snapping", "wood", "bare", "weathered" };
    }
}
