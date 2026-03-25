using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Hazel Undergrowth - Dense hazel bushes beneath the canopy.
/// </summary>
public class HazelUndergrowthNode : NarrationNode
{
    public override string NodeId => "hazel_undergrowth";
    public override string ContextDescription => "searching the hazel undergrowth";
    public override string TransitionDescription => "push into the hazels";
    public override bool IsEntryNode => false;
    
    public override List<string> NodeKeywords => new() { "catkin", "nut", "bush", "undergrowth" };
    
    private static readonly string[] Moods = { "dense", "tangled", "productive", "crowded", "bushy", "nutty", "thriving", "clustered" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} hazel undergrowth";
    }
    
    public sealed class Hazelnut : Item
    {
        public override string ItemId => "hazelnut";
        public override string DisplayName => "Hazelnut";
        public override string Description => "A ripe hazelnut in its leafy husk";
        public override List<string> OutcomeKeywords => new() { "nut", "shell", "husk" };
    }
    
    public sealed class HazelCatkin : Item
    {
        public override string ItemId => "hazel_undergrowth_catkin";
        public override string DisplayName => "Hazel Catkin";
        public override string Description => "A yellow male catkin hanging from a branch";
        public override List<string> OutcomeKeywords => new() { "catkin", "pollen", "spring" };
    }
}
