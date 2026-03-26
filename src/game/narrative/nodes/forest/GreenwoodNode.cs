using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Greenwood - Level 3. Mixed hardwood forest with hazel and fungal rings.
/// </summary>
public class GreenwoodNode : NarrationNode
{
    public override string NodeId => "greenwood";
    public override string ContextDescription => "walking through greenwood";
    public override string TransitionDescription => "enter the greenwood";
    public override bool IsEntryNode => true;
    
    public override List<string> NodeKeywords => new() { "hazel", "fungus", "moss", "vitality" };
    
    private static readonly string[] Moods = { "thriving", "lush", "verdant", "rich", "vibrant", "living", "dense", "flourishing" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} greenwood";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"walking through a {mood} greenwood";
    }
    
    public sealed class HazelNuts : Item
    {
        public override string ItemId => "greenwood_hazel_nuts";
        public override string DisplayName => "Hazel Nuts";
        public override string Description => "Small brown nuts from greenwood hazel trees";
        public override List<string> OutcomeKeywords => new() { "caryopsis", "shell", "corylus", "cluster" };
    }
}
