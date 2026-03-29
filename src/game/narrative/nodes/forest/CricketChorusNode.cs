using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Cricket Chorus - A location where crickets sing loudly.
/// </summary>
public class CricketChorusNode : NarrationNode
{
    public override string NodeId => "cricket_chorus";
    public override string ContextDescription => "listening to the cricket chorus";
    public override string TransitionDescription => "follow the cricket sounds";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a dry repetitive <chirp> from every direction"), KeywordInContext.Parse("the swelling <chorus> rising around the clearing"), KeywordInContext.Parse("a steady <rhythm> pulsing through the dusk") };
    
    private static readonly string[] Moods = { "chirping", "rhythmic", "singing", "loud", "harmonious", "nocturnal", "resonant", "trilling" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} cricket chorus";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"listening to a {mood} cricket chorus";
    }
    
    public override List<Item> GetItems() => new() { new CricketLegs() };

    public sealed class CricketLegs : Item
    {
        public override string ItemId => "cricket_chorus_cricket_legs";
        public override string DisplayName => "Cricket Legs";
        public override string Description => "Jointed legs from dead crickets";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a segmented <joint> from a cricket leg"), KeywordInContext.Parse("a slender <appendage> broken from the body"), KeywordInContext.Parse("the brittle <chitin> shell of a dead cricket") };
    }
    
}
