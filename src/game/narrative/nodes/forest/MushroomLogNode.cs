using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Mushroom Log - A decaying log covered in shelf mushrooms.
/// </summary>
public class MushroomLogNode : NarrationNode
{
    public override string NodeId => "mushroom_log";
    public override string ContextDescription => "examining mushrooms on the log";
    public override string TransitionDescription => "inspect the mushroom log";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "bracket", "fungus", "decay", "wood" };
    
    private static readonly string[] Moods = { "layered", "shelf-like", "overlapping", "decaying", "fungal", "rotting", "tiered", "clustered" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} mushroom log";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} mushroom log";
    }
    
    public sealed class ShelfMushroom : Item
    {
        public override string ItemId => "shelf_mushroom";
        public override string DisplayName => "Shelf Mushroom";
        public override string Description => "A tough, bracket-like shelf mushroom";
        public override List<string> OutcomeKeywords => new() { "mushroom", "bracket", "shelf" };
    }
    
    public sealed class DecayedLogWood : Item
    {
        public override string ItemId => "mushroom_log_rotten_wood";
        public override string DisplayName => "Rotten Wood";
        public override string Description => "Soft, decaying wood riddled with fungal threads";
        public override List<string> OutcomeKeywords => new() { "wood", "decay", "mycelium" };
    }
    
    public sealed class BeetleHole : Item
    {
        public override string ItemId => "mushroom_log_beetle_hole";
        public override string DisplayName => "Beetle Gallery";
        public override string Description => "Network of beetle tunnels carved through the decaying wood";
        public override List<string> OutcomeKeywords => new() { "gallery", "tunnel", "larvae" };
    }
}
