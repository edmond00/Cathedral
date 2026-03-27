using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Lowwood - Level 6. Moist woodland with alder and fungal growth.
/// </summary>
public class LowwoodNode : NarrationNode
{
    public override string NodeId => "lowwood";
    public override string ContextDescription => "treading through lowwood";
    public override string TransitionDescription => "descend into the lowwood";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("an <alder> tree standing ankle-deep in water"), KeywordInContext.Parse("some tall <reed>s edging a standing pool"), KeywordInContext.Parse("a wide <puddle> blocking the path forward"), KeywordInContext.Parse("an oppressive <dampness> soaking the clothing") };
    
    private static readonly string[] Moods = { "damp", "soggy", "humid", "waterlogged", "misty", "moist", "dripping", "wet" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} lowwood";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"treading through a {mood} lowwood";
    }
    
    public sealed class WetMoss : Item
    {
        public override string ItemId => "lowwood_wet_moss";
        public override string DisplayName => "Wet Moss";
        public override string Description => "Damp moss gathered from lowwood ground";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a dripping <bryophyte> from the lowwood ground"), KeywordInContext.Parse("some soaking wet moss full of <dampness>") };
    }
    
    public sealed class PuddleWater : Item
    {
        public override string ItemId => "lowwood_puddle_water";
        public override string DisplayName => "Puddle Water";
        public override string Description => "Water collected from lowwood puddles";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the muddy <runoff> collected from higher ground"), KeywordInContext.Parse("a shallow <basin> of standing lowwood water") };
    }
}
