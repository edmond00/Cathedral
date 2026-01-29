using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Root Path - A transversal feature of interwoven roots forming a natural pathway.
/// </summary>
public class RootPathNode : NarrationNode
{
    public override string NodeId => "root_path";
    public override string ContextDescription => "walking the root path";
    public override string TransitionDescription => "take the root path";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "twisted", "roots", "network", "interwoven", "gnarled", "knotted", "raised", "natural", "pathway", "woody" };
    
    private static readonly string[] Moods = { "twisted", "winding", "serpentine", "tangled", "knotted", "interlaced", "convoluted", "meandering" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} root path";
    }
    
    public sealed class BarkChunk : Item
    {
        public override string ItemId => "root_bark_chunk";
        public override string DisplayName => "Root Bark Chunk";
        public override string Description => "A piece of rough bark from the exposed roots";
        public override List<string> OutcomeKeywords => new() { "rough", "textured", "brown", "fibrous", "woody", "dry", "flaky", "thick", "bark", "cork-like" };
    }
}
