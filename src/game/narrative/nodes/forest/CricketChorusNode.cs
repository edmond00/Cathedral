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
    
    public override List<string> NodeKeywords => new() { "chirping", "rhythmic", "singing", "insects", "night", "chorus", "loud", "trilling", "music", "sound" };
    
    private static readonly string[] Moods = { "chirping", "rhythmic", "singing", "loud", "harmonious", "nocturnal", "resonant", "trilling" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} cricket chorus";
    }
    
    public sealed class CricketLegs : Item
    {
        public override string ItemId => "cricket_chorus_cricket_legs";
        public override string DisplayName => "Cricket Legs";
        public override string Description => "Jointed legs from dead crickets";
        public override List<string> OutcomeKeywords => new() { "legs", "jointed", "spindly", "brown", "thin", "segments", "insect", "dried", "delicate", "appendages" };
    }
    
    public sealed class CricketChirp : Item
    {
        public override string ItemId => "cricket_chorus_cricket_chirp_sample";
        public override string DisplayName => "Cricket Chirp Sample";
        public override string Description => "A living cricket captured for its song";
        public override List<string> OutcomeKeywords => new() { "cricket", "chirping", "singing", "sound", "rhythmic", "musical", "nocturnal", "calling", "insect", "alive" };
    }
}
