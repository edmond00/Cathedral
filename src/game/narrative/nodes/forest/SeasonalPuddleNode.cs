using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Seasonal Puddle - A temporary pool of water.
/// Associated with: Lowwood
/// </summary>
public class SeasonalPuddleNode : NarrationNode
{
    public override string NodeId => "seasonal_puddle";
    public override string ContextDescription => "examining the seasonal puddle";
    public override string TransitionDescription => "approach the puddle";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "water", "reflection", "shallow", "temporary", "muddy", "edges", "wet", "pool", "still", "murky" };
    
    private static readonly string[] Moods = { "shallow", "murky", "still", "reflective", "temporary", "muddy", "stagnant", "rain-filled" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} seasonal puddle";
    }
    
    public sealed class PuddleMud : Item
    {
        public override string ItemId => "puddle_mud";
        public override string DisplayName => "Puddle Mud";
        public override string Description => "Wet, sticky mud from the puddle edge";
        public override List<string> OutcomeKeywords => new() { "wet", "sticky", "brown", "mud", "clay", "squelching", "soft", "dark", "moist", "earthy" };
    }
}
