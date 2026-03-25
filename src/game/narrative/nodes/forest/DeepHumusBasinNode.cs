using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Deep Humus Basin - A depression filled with rich, decomposed organic matter.
/// Associated with: Oldgrowth
/// </summary>
public class DeepHumusBasinNode : NarrationNode
{
    public override string NodeId => "deep_humus_basin";
    public override string ContextDescription => "exploring the humus basin";
    public override string TransitionDescription => "descend into the basin";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "humus", "basin", "fertility", "decay" };
    
    private static readonly string[] Moods = { "deep", "rich", "black", "fertile", "accumulated", "organic", "soft", "ancient" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} deep humus basin";
    }
    
    public sealed class PureHumus : Item
    {
        public override string ItemId => "pure_humus";
        public override string DisplayName => "Pure Humus";
        public override string Description => "Black, crumbly humus from centuries of decay";
        public override List<string> OutcomeKeywords => new() { "humus", "soil", "fertility", "decomposition" };
    }
    
    public sealed class AncientSeed : Item
    {
        public override string ItemId => "deep_humus_ancient_seed";
        public override string DisplayName => "Ancient Seed";
        public override string Description => "A long-buried seed preserved in the humus";
        public override List<string> OutcomeKeywords => new() { "seed", "dormancy", "shell" };
    }
}
