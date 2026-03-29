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
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a pale <lichen> coating the trunk surface"), KeywordInContext.Parse("the rough <bark> encrusted with growth"), KeywordInContext.Parse("a grey-green <crust> pressed into the wood"), KeywordInContext.Parse("an ancient <symbiosis> written into the bark") };
    
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
    
    public override List<Item> GetItems() => new() { new LichenCrust(), new LichenDust() };

    public sealed class LichenCrust : Item
    {
        public override string ItemId => "lichen_crust";
        public override string DisplayName => "Lichen Crust";
        public override string Description => "A piece of lichen-covered bark";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the flat grey <thallus> of the lichen"), KeywordInContext.Parse("a pale <pigment> colouring the crust") };
    }
    
    public sealed class LichenDust : Item
    {
        public override string ItemId => "lichen_bark_soredia";
        public override string DisplayName => "Lichen Soredia";
        public override string Description => "Fine reproductive dust from lichen bodies";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a puff of fine lichen <dust> in the air"), KeywordInContext.Parse("some <spore>-bearing soredia from the crust") };
    }
}
