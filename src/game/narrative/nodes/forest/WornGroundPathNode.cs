using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Worn Ground Path - A transversal feature of a well-trodden trail.
/// </summary>
public class WornGroundPathNode : NarrationNode
{
    public override string NodeId => "worn_ground_path";
    public override string ContextDescription => "following the worn path";
    public override string TransitionDescription => "take the worn path";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a worn <trail> through the forest floor"), KeywordInContext.Parse("the bare <earth> beneath many feet"), KeywordInContext.Parse("a fine <dust> rising from the path"), KeywordInContext.Parse("the hard <compaction> of the well-trodden ground") };
    
    private static readonly string[] Moods = { "well-worn", "trampled", "smooth", "clear", "obvious", "easy", "traveled", "beaten" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} worn ground path";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"following a {mood} worn ground path";
    }
    
    public sealed class PathDust : Item
    {
        public override string ItemId => "worn_ground_path_path_dust";
        public override string DisplayName => "Path Dust";
        public override string Description => "Fine dust from the heavily traveled path";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a fine <silt> coating every surface"), KeywordInContext.Parse("a dry <powder> of crushed earth"), KeywordInContext.Parse("the <artery> of movement through the forest") };
    }
}
