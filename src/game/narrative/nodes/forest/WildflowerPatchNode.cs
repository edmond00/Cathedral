using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Wildflower Patch - A colorful spread of forest wildflowers.
/// </summary>
public class WildflowerPatchNode : NarrationNode
{
    public override string NodeId => "wildflower_patch";
    public override string ContextDescription => "admiring the wildflowers";
    public override string TransitionDescription => "approach the flowers";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "colorful", "petals", "blooms", "fragrant", "delicate", "purple", "yellow", "white", "nectar", "vibrant" };
    
    private static readonly string[] Moods = { "blooming", "colorful", "fragrant", "vibrant", "bright", "cheerful", "delicate", "beautiful" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} wildflower patch";
    }
    
    public sealed class WildflowerBouquet : Item
    {
        public override string ItemId => "wildflower_bouquet";
        public override string DisplayName => "Wildflower Bouquet";
        public override string Description => "A handful of picked wildflowers";
        public override List<string> OutcomeKeywords => new() { "colorful", "fragrant", "fresh", "delicate", "mixed", "petals", "stems", "vibrant", "flowers", "bouquet" };
    }
}
