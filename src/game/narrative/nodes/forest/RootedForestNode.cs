using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Rooted Forest - Level 12. Exposed root systems and erosion channels.
/// </summary>
public class RootedForestNode : NarrationNode
{
    public override string NodeId => "rooted_forest";
    public override string ContextDescription => "climbing through the rooted forest";
    public override string TransitionDescription => "enter the rooted forest";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "root", "web", "erosion", "network" };
    
    private static readonly string[] Moods = { "gnarled", "exposed", "twisted", "contorted", "webbed", "interlaced", "serpentine", "convoluted" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} rooted forest";
    }
    
    public sealed class ExposedRootFiber : Item
    {
        public override string ItemId => "rooted_forest_exposed_root_fiber";
        public override string DisplayName => "Exposed Root Fiber";
        public override string Description => "Fibrous strands from massive exposed roots";
        public override List<string> OutcomeKeywords => new() { "root", "fiber", "wood" };
    }
    
    public sealed class RootGrip : Item
    {
        public override string ItemId => "rooted_forest_root_grip";
        public override string DisplayName => "Root Grip";
        public override string Description => "Sturdy root section useful for climbing";
        public override List<string> OutcomeKeywords => new() { "root", "grip", "handhold" };
    }
}
