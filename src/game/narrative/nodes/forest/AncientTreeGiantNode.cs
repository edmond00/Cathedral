using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Ancient Tree Giant - A truly massive, centuries-old tree.
/// Associated with: Oldgrowth
/// </summary>
public class AncientTreeGiantNode : NarrationNode
{
    public override string NodeId => "ancient_tree_giant";
    public override string ContextDescription => "standing before the ancient giant";
    public override string TransitionDescription => "approach the giant tree";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "massive", "ancient", "centuries", "enormous", "trunk", "girth", "towering", "patriarch", "old", "venerable" };
    
    private static readonly string[] Moods = { "massive", "ancient", "venerable", "primeval", "enormous", "patriarch", "timeless", "monumental" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} ancient tree giant";
    }
    
    public sealed class AncientBark : Item
    {
        public override string ItemId => "ancient_bark";
        public override string DisplayName => "Ancient Bark";
        public override string Description => "A piece of deeply furrowed bark from the ancient tree";
        public override List<string> OutcomeKeywords => new() { "thick", "furrowed", "ancient", "rough", "bark", "aged", "weathered", "deep", "textured", "hard" };
    }
}
