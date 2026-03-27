using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Wildflower Patch - A colorful spread of forest wildflowers.
/// </summary>
public class WildflowerPatchNode : NarrationNode
{
    public override string NodeId => "wildflower_patch";
    public override string ContextDescription => "admiring the wildflowers";
    public override string TransitionDescription => "approach the flowers";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a bright loose <petal> fallen to the ground below"), KeywordInContext.Parse("the full open <bloom> of a flower facing the light"), KeywordInContext.Parse("a bee moving slowly toward the sweet <nectar>"), KeywordInContext.Parse("a heady <fragrance> drifting across the clearing") };
    
    private static readonly string[] Moods = { "blooming", "colorful", "fragrant", "vibrant", "bright", "cheerful", "delicate", "beautiful" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} wildflower patch";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"admiring a {mood} wildflower patch";
    }
    
    public sealed class WildflowerBouquet : Item
    {
        public override string ItemId => "wildflower_bouquet";
        public override string DisplayName => "Wildflower Bouquet";
        public override string Description => "A handful of picked wildflowers";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a small fragrant <nosegay> of picked wildflowers"), KeywordInContext.Parse("some loose <petal>s already wilting in the hand"), KeywordInContext.Parse("a mixed <flower> bunch gathered from the colourful spread") };
    }
    
    public sealed class FlowerPetal : Item
    {
        public override string ItemId => "wildflower_patch_petal";
        public override string DisplayName => "Flower Petal";
        public override string Description => "A single delicate wildflower petal";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a single undifferentiated <tepal> from a simple wildflower"), KeywordInContext.Parse("the smooth <silk>-like texture of the petal surface"), KeywordInContext.Parse("the last trace of a faded <bloom> pressed flat") };
    }
    
    public sealed class ButterflyWing : Item
    {
        public override string ItemId => "wildflower_patch_butterfly_wing";
        public override string DisplayName => "Butterfly Wing";
        public override string Description => "A shed butterfly wing dusted with colorful scales";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the papery delicate <ala> of a butterfly wing still intact"), KeywordInContext.Parse("a dust of iridescent <scale>s rubbed off on the finger"), KeywordInContext.Parse("the translucent <membrane> between the wing veins") };
    }
}
