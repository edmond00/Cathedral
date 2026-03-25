using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Mossy Stone Outcrop - Ancient stones covered in thick moss.
/// </summary>
public class MossyStoneOutcropNode : NarrationNode
{
    public override string NodeId => "mossy_stone_outcrop";
    public override string ContextDescription => "examining the mossy outcrop";
    public override string TransitionDescription => "climb the outcrop";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "moss", "stone", "rock", "velvet" };
    
    private static readonly string[] Moods = { "moss-covered", "ancient", "verdant", "soft", "cushioned", "green", "damp", "thick" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} mossy stone outcrop";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} mossy stone outcrop";
    }
    
    public sealed class OutcropMoss : Item
    {
        public override string ItemId => "outcrop_moss";
        public override string DisplayName => "Thick Moss";
        public override string Description => "A cushion of deep green moss from the stones";
        public override List<string> OutcomeKeywords => new() { "moss", "cushion", "velvet" };
    }
    
    public sealed class AncientStone : Item
    {
        public override string ItemId => "mossy_outcrop_ancient_stone";
        public override string DisplayName => "Ancient Stone";
        public override string Description => "A weathered piece of the ancient rock outcrop";
        public override List<string> OutcomeKeywords => new() { "stone", "granite", "mineral" };
    }
}
