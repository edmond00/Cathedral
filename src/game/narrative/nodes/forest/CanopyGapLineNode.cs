using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Canopy Gap Line - A transversal feature where light penetrates the forest ceiling.
/// </summary>
public class CanopyGapLineNode : NarrationNode
{
    public override string NodeId => "canopy_gap_line";
    public override string ContextDescription => "walking through the canopy gap";
    public override string TransitionDescription => "follow the light gap";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "light", "shaft", "bright", "opening", "sunshine", "ray", "illuminated", "warm", "golden", "gap" };
    
    private static readonly string[] Moods = { "bright", "sunlit", "illuminated", "radiant", "gleaming", "golden", "dappled", "shimmering" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} canopy gap line";
    }
    
    public sealed class SunwarmLeaves : Item
    {
        public override string ItemId => "sunwarm_leaves";
        public override string DisplayName => "Sunwarm Leaves";
        public override string Description => "Fresh leaves warmed by direct sunlight";
        public override List<string> OutcomeKeywords => new() { "warm", "green", "fresh", "bright", "tender", "illuminated", "photosynthetic", "vibrant", "leaf", "living" };
    }
}
