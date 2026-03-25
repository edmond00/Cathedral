using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Alder Grove - A stand of alder trees in moist ground.
/// </summary>
public class AlderGroveNode : NarrationNode
{
    public override string NodeId => "alder_grove";
    public override string ContextDescription => "walking through the alder grove";
    public override string TransitionDescription => "enter the alders";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "catkin", "bark", "nitrogen", "grove" };
    
    private static readonly string[] Moods = { "damp", "grey", "quiet", "moist", "cool", "shadowy", "nitrogen-rich", "serene" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} alder grove";
    }
    
    public sealed class AlderCone : Item
    {
        public override string ItemId => "alder_cone";
        public override string DisplayName => "Alder Cone";
        public override string Description => "A small woody cone from an alder tree";
        public override List<string> OutcomeKeywords => new() { "cone", "seed", "bark" };
    }
    
    public sealed class AlderCatkin : Item
    {
        public override string ItemId => "alder_grove_catkin";
        public override string DisplayName => "Alder Catkin";
        public override string Description => "A dangling catkin from an alder branch";
        public override List<string> OutcomeKeywords => new() { "pollen", "catkin", "flower" };
    }
}
