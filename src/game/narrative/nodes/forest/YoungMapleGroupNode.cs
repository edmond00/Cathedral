using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Young Maple Group - A cluster of young maple saplings.
/// Associated with: Brightwood
/// </summary>
public class YoungMapleGroupNode : NarrationNode
{
    public override string NodeId => "young_maple_group";
    public override string ContextDescription => "examining the young maples";
    public override string TransitionDescription => "approach the maples";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "sapling", "palmate", "leaves", "young", "green", "cluster", "slender", "growing", "vibrant", "fresh" };
    
    private static readonly string[] Moods = { "vigorous", "young", "thriving", "verdant", "growing", "clustered", "healthy", "vibrant" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} young maple group";
    }
    
    public sealed class MapleSeed : Item
    {
        public override string ItemId => "maple_seed";
        public override string DisplayName => "Maple Seed";
        public override string Description => "A winged maple seed, ready to spin";
        public override List<string> OutcomeKeywords => new() { "winged", "spinning", "helicopter", "seed", "brown", "paired", "samara", "propeller", "flying", "light" };
    }
}
