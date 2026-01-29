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
}
