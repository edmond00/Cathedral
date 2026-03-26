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
    
    public override List<string> NodeKeywords => new() { "pile", "rot", "decay", "branches" };
    
    private static readonly string[] Moods = { "collapsed", "tangled", "dead", "brittle", "chaotic", "decaying", "piled", "grey" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} deadwood heap";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"climbing over a {mood} deadwood heap";
    }
    
    public sealed class DeadBranch : Item
    {
        public override string ItemId => "dead_branch";
        public override string DisplayName => "Dead Branch";
        public override string Description => "A brittle, lifeless branch from the heap";
        public override List<string> OutcomeKeywords => new() { "bough", "wood", "decay" };
    }
    
    public sealed class DryFungus : Item
    {
        public override string ItemId => "deadwood_heap_dry_fungus";
        public override string DisplayName => "Dry Fungus";
        public override string Description => "A papery bracket fungus from decaying wood";
        public override List<string> OutcomeKeywords => new() { "bracket", "shelf", "conk" };
    }
    
    public sealed class BarkBeetle : Item
    {
        public override string ItemId => "deadwood_heap_bark_beetle";
        public override string DisplayName => "Bark Beetle Specimen";
        public override string Description => "Dead bark beetles found within the crumbling wood";
        public override List<string> OutcomeKeywords => new() { "scolytid", "carapace", "chitin" };
    }
}
