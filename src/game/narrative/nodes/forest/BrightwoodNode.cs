using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Brightwood - Level 2. Light-filled woodland with beech trees and ferns.
/// </summary>
public class BrightwoodNode : NarrationNode
{
    public override string NodeId => "brightwood";
    public override string ContextDescription => "wandering through brightwood";
    public override string TransitionDescription => "enter the brightwood";
    public override bool IsEntryNode => true;
    
    public override List<string> NodeKeywords => new() { "beech", "fern", "light", "brightness" };
    
    private static readonly string[] Moods = { "radiant", "gleaming", "shimmering", "bright", "fresh", "vibrant", "golden", "cheerful" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"{mood} brightwood";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"wandering through a {mood} brightwood";
    }
    
    public sealed class BeechLeaves : Item
    {
        public override string ItemId => "brightwood_beech_leaves";
        public override string DisplayName => "Beech Leaves";
        public override string Description => "Golden-green leaves from brightwood beech trees";
        public override List<string> OutcomeKeywords => new() { "leaf", "beech", "vein" };
    }
}
