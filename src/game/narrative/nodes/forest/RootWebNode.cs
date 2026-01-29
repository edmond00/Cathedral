using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Root Web - An intricate network of exposed surface roots.
/// Associated with: RootedForest
/// </summary>
public class RootWebNode : NarrationNode
{
    public override string NodeId => "root_web";
    public override string ContextDescription => "navigating the root web";
    public override string TransitionDescription => "step through the root web";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "network", "interwoven", "exposed", "surface", "complex", "roots", "tangled", "maze", "spreading", "interconnected" };
    
    private static readonly string[] Moods = { "intricate", "interwoven", "complex", "network", "maze-like", "tangled", "spreading", "interconnected" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} root web";
    }
    
    public sealed class RootFiber : Item
    {
        public override string ItemId => "root_fiber";
        public override string DisplayName => "Root Fibers";
        public override string Description => "Thin, tough fibers from surface roots";
        public override List<string> OutcomeKeywords => new() { "thin", "fibrous", "tough", "string", "root", "brown", "strong", "flexible", "wiry", "threadlike" };
    }
}
