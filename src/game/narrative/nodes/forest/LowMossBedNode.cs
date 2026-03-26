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
    
    public override List<string> NodeKeywords => new() { "moss", "carpet", "silence", "dampness" };
    
    private static readonly string[] Moods = { "soft", "silent", "cushioned", "thick", "velvety", "damp", "quiet", "green" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} low moss bed";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"walking on a {mood} low moss bed";
    }
    
    public sealed class MossCarpet : Item
    {
        public override string ItemId => "moss_carpet";
        public override string DisplayName => "Moss Carpet";
        public override string Description => "A thick sheet of forest floor moss";
        public override List<string> OutcomeKeywords => new() { "cushion", "velvet", "dampness" };
    }
    
    public sealed class MossSpore : Item
    {
        public override string ItemId => "low_moss_spore_capsule";
        public override string DisplayName => "Moss Spore Capsule";
        public override string Description => "Tiny capsules on thin stalks containing spores";
        public override List<string> OutcomeKeywords => new() { "sporophyte", "stalk", "propagule" };
    }
}
