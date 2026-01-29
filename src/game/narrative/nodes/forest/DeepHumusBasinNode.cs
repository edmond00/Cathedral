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
    
    public override List<string> NodeKeywords => new() { "deep", "black", "rich", "decomposed", "organic", "basin", "fertile", "dark", "soft", "accumulated" };
    
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
        public override List<string> OutcomeKeywords => new() { "black", "crumbly", "rich", "organic", "humus", "fertile", "ancient", "decomposed", "pure", "soil" };
    }
}
