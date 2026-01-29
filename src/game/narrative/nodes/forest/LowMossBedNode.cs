using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Low Moss Bed - A carpet of moss covering the dark forest floor.
/// Associated with: Shadowwood
/// </summary>
public class LowMossBedNode : NarrationNode
{
    public override string NodeId => "low_moss_bed";
    public override string ContextDescription => "walking on the moss bed";
    public override string TransitionDescription => "step onto the moss";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "soft", "carpet", "green", "cushiony", "damp", "moss", "silent", "thick", "covering", "velvet" };
    
    private static readonly string[] Moods = { "soft", "silent", "cushioned", "thick", "velvety", "damp", "quiet", "green" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} low moss bed";
    }
    
    public sealed class MossCarpet : Item
    {
        public override string ItemId => "moss_carpet";
        public override string DisplayName => "Moss Carpet";
        public override string Description => "A thick sheet of forest floor moss";
        public override List<string> OutcomeKeywords => new() { "soft", "green", "thick", "damp", "sheet", "moss", "velvety", "cushiony", "living", "carpet" };
    }
}
