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
    
    public override List<string> NodeKeywords => new() { "pool", "mud", "reflection", "temporality" };
    
    private static readonly string[] Moods = { "shallow", "murky", "still", "reflective", "temporary", "muddy", "stagnant", "rain-filled" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} seasonal puddle";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} seasonal puddle";
    }
    
    public sealed class PuddleMud : Item
    {
        public override string ItemId => "puddle_mud";
        public override string DisplayName => "Puddle Mud";
        public override string Description => "Wet, sticky mud from the puddle edge";
        public override List<string> OutcomeKeywords => new() { "silt", "clay", "muck" };
    }
    
    public sealed class TadpoleWater : Item
    {
        public override string ItemId => "seasonal_puddle_tadpole_water";
        public override string DisplayName => "Tadpole Water";
        public override string Description => "Water sample teeming with tiny tadpoles";
        public override List<string> OutcomeKeywords => new() { "polliwog", "larvae", "spawn" };
    }
}
