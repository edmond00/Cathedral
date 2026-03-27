using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Butterfly Glade - A sunny spot where butterflies gather.
/// </summary>
public class ButterflyGladeNode : NarrationNode
{
    public override string NodeId => "butterfly_glade";
    public override string ContextDescription => "watching butterflies in the glade";
    public override string TransitionDescription => "enter the butterfly glade";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a bright <wing> flashing in the sunlight"), KeywordInContext.Parse("some open <flower>s drawing the butterflies in"), KeywordInContext.Parse("the sweet <nectar> they visit flower to flower for"), KeywordInContext.Parse("an open sunny <glade> ahead") };
    
    private static readonly string[] Moods = { "colorful", "dancing", "fluttering", "bright", "lively", "vibrant", "magical", "enchanting" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} butterfly glade";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"wandering in a {mood} butterfly glade";
    }
    
    public sealed class ButterflyWings : Item
    {
        public override string ItemId => "butterfly_glade_butterfly_wings";
        public override string DisplayName => "Butterfly Wings";
        public override string Description => "Colorful wings shed by butterflies";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a tiny <scale> from the butterfly wing"), KeywordInContext.Parse("some coloured <powder> dusting the fingers"), KeywordInContext.Parse("a geometric <pattern> pressed into the membrane") };
    }
    
    public sealed class Nectar : Item
    {
        public override string ItemId => "butterfly_glade_nectar";
        public override string DisplayName => "Flower Nectar";
        public override string Description => "Sweet nectar from glade flowers";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a faint <sweetness> on the fingertip"), KeywordInContext.Parse("some pale <pollen> clinging to the petals") };
    }
}
