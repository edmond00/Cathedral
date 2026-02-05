using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Highwood - Level 4. Tall-trunked forest with sparse understory.
/// </summary>
public class HighwoodNode : NarrationNode
{
    public override string NodeId => "highwood";
    public override string ContextDescription => "walking beneath the highwood";
    public override string TransitionDescription => "enter the highwood";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "tall", "soaring", "sparse", "lichen", "bark", "giants", "towering", "exposed", "roots", "vertical" };
    
    private static readonly string[] Moods = { "towering", "majestic", "imposing", "grand", "austere", "noble", "dominant", "lofty" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} highwood";
    }
    
    public sealed class HighwoodLichen : Item
    {
        public override string ItemId => "highwood_lichen";
        public override string DisplayName => "Highwood Lichen";
        public override string Description => "Pale lichen scraped from highwood bark";
        public override List<string> OutcomeKeywords => new() { "lichen", "pale", "bark", "crusty", "gray", "textured", "growth", "dry", "ancient", "crumbling" };
    }
    
    public sealed class BarkShavings : Item
    {
        public override string ItemId => "highwood_bark_shavings";
        public override string DisplayName => "Bark Shavings";
        public override string Description => "Thin strips of bark peeled from tall trunks";
        public override List<string> OutcomeKeywords => new() { "bark", "shavings", "strips", "thin", "brown", "fibrous", "peeled", "curled", "dry", "woody" };
    }
}
