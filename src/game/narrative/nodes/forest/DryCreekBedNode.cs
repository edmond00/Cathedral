using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Dry Creek Bed - A transversal feature of an empty watercourse.
/// </summary>
public class DryCreekBedNode : NarrationNode
{
    public override string NodeId => "dry_creek_bed";
    public override string ContextDescription => "walking along the dry creek bed";
    public override string TransitionDescription => "follow the dry creek";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "dry", "stones", "channel", "empty", "smooth", "polished", "weathered", "bed", "depression", "dusty" };
    
    private static readonly string[] Moods = { "parched", "barren", "empty", "desiccated", "arid", "waterless", "dusty", "desolate" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} dry creek bed";
    }
    
    public sealed class SmoothStone : Item
    {
        public override string ItemId => "smooth_creek_stone";
        public override string DisplayName => "Smooth Creek Stone";
        public override string Description => "A water-polished stone from the ancient creek bed";
        public override List<string> OutcomeKeywords => new() { "polished", "smooth", "round", "grey", "weathered", "worn", "cold", "heavy", "stone", "hard" };
    }
}
