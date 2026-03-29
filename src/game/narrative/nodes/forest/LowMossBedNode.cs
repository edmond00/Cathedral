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
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a thick green <moss> covering the ground"), KeywordInContext.Parse("the soft yielding <carpet> of the forest floor"), KeywordInContext.Parse("a <silence> broken only by dripping water"), KeywordInContext.Parse("the cool <dampness> soaking through the boots") };
    
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
    
    public override List<Item> GetItems() => new() { new MossCarpet(), new MossSpore() };

    public sealed class MossCarpet : Item
    {
        public override string ItemId => "moss_carpet";
        public override string DisplayName => "Moss Carpet";
        public override string Description => "A thick sheet of forest floor moss";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a spongy <cushion> of deep forest moss"), KeywordInContext.Parse("the soft <velvet> texture of the moss mat") };
    }
    
    public sealed class MossSpore : Item
    {
        public override string ItemId => "low_moss_spore_capsule";
        public override string DisplayName => "Moss Spore Capsule";
        public override string Description => "Tiny capsules on thin stalks containing spores";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the tiny <sporophyte> rising above the moss"), KeywordInContext.Parse("a thin red <stalk> bearing the spore capsule") };
    }
}
