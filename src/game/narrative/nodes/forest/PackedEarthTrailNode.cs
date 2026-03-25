using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Packed Earth Trail - A transversal feature of hardened, traveled ground.
/// </summary>
public class PackedEarthTrailNode : NarrationNode
{
    public override string NodeId => "packed_earth_trail";
    public override string ContextDescription => "walking the packed earth trail";
    public override string TransitionDescription => "follow the trail";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "trail", "earth", "compaction", "path" };
    
    private static readonly string[] Moods = { "firm", "solid", "reliable", "hard", "stable", "compacted", "dependable", "sturdy" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} packed earth trail";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"walking a {mood} packed earth trail";
    }
    
    public sealed class HardClay : Item
    {
        public override string ItemId => "packed_earth_trail_hard_clay";
        public override string DisplayName => "Hard Clay";
        public override string Description => "Compacted clay from the trail surface";
        public override List<string> OutcomeKeywords => new() { "clay", "earth", "compaction" };
    }
}
