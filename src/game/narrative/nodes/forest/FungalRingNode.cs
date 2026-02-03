using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Fungal Ring - A mysterious circle of mushrooms.
/// </summary>
public class FungalRingNode : NarrationNode
{
    public override string NodeId => "fungal_ring";
    public override string ContextDescription => "observing the fungal ring";
    public override string TransitionDescription => "approach the ring";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "circle", "mushrooms", "caps", "white", "mysterious", "spores", "mycelium", "pattern", "fairy", "ring" };
    
    private static readonly string[] Moods = { "mysterious", "perfect", "uncanny", "circular", "strange", "symmetrical", "eerie", "magical" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} fungal ring";
    }
    
    public sealed class FairyRingMushroom : Item
    {
        public override string ItemId => "fairy_ring_mushroom";
        public override string DisplayName => "Fairy Ring Mushroom";
        public override string Description => "A white mushroom from the mysterious circle";
        public override List<string> OutcomeKeywords => new() { "white", "cap", "gills", "stem", "pale", "delicate", "mushroom", "fungus", "spores", "small" };
    }
    
    public sealed class MyceliumThread : Item
    {
        public override string ItemId => "fungal_ring_mycelium";
        public override string DisplayName => "Mycelium Thread";
        public override string Description => "White fungal threads just beneath the soil";
        public override List<string> OutcomeKeywords => new() { "white", "threads", "network", "underground", "filaments", "spreading", "web", "root", "branching", "hidden" };
    }
    
    public sealed class SporeCloud : Item
    {
        public override string ItemId => "fungal_ring_spore_cloud";
        public override string DisplayName => "Spore Cloud Sample";
        public override string Description => "A faint puff of microscopic spores released from disturbed caps";
        public override List<string> OutcomeKeywords => new() { "dusty", "floating", "microscopic", "powder", "white", "airborne", "dispersing", "cloud", "reproductive", "fine" };
    }
}
