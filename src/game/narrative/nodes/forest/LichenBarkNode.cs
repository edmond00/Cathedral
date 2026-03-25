using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Lichen Bark - Tall trees with lichen-covered bark.
/// Associated with: Highwood
/// </summary>
public class LichenBarkNode : NarrationNode
{
    public override string NodeId => "lichen_bark";
    public override string ContextDescription => "examining lichen-covered bark";
    public override string TransitionDescription => "approach the lichened trunks";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "lichen", "bark", "crust", "symbiosis" };
    
    private static readonly string[] Moods = { "encrusted", "patterned", "ancient", "textured", "weathered", "colonized", "aged", "symbiotic" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} lichen bark";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} lichen bark";
    }
    
    public sealed class LichenCrust : Item
    {
        public override string ItemId => "lichen_crust";
        public override string DisplayName => "Lichen Crust";
        public override string Description => "A piece of lichen-covered bark";
        public override List<string> OutcomeKeywords => new() { "lichen", "crust", "bark" };
    }
    
    public sealed class LichenDust : Item
    {
        public override string ItemId => "lichen_bark_soredia";
        public override string DisplayName => "Lichen Soredia";
        public override string Description => "Fine reproductive dust from lichen bodies";
        public override List<string> OutcomeKeywords => new() { "dust", "spore", "dispersal" };
    }
}
