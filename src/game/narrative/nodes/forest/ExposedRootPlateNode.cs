using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Exposed Root Plate - Upturned roots of a fallen giant.
/// Associated with: Highwood
/// </summary>
public class ExposedRootPlateNode : NarrationNode
{
    public override string NodeId => "exposed_root_plate";
    public override string ContextDescription => "climbing the root plate";
    public override string TransitionDescription => "climb the exposed roots";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "root", "earth", "wall", "network" };
    
    private static readonly string[] Moods = { "massive", "upturned", "towering", "vertical", "exposed", "torn", "dramatic", "impressive" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} exposed root plate";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"climbing a {mood} exposed root plate";
    }
    
    public sealed class ClayClod : Item
    {
        public override string ItemId => "root_clay_clod";
        public override string DisplayName => "Clay Clod";
        public override string Description => "A clump of clay from the root plate";
        public override List<string> OutcomeKeywords => new() { "clay", "clod", "mineral" };
    }
    
    public sealed class RootFiber : Item
    {
        public override string ItemId => "exposed_root_fiber";
        public override string DisplayName => "Root Fiber";
        public override string Description => "Stringy root fibers torn from the earth";
        public override List<string> OutcomeKeywords => new() { "root", "fiber", "thread", "network" };
    }
}
