using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Alder Grove - A stand of alder trees in moist ground.
/// </summary>
public class AlderGroveNode : NarrationNode
{
    public override string NodeId => "alder_grove";
    public override string ContextDescription => "walking through the alder grove";
    public override string TransitionDescription => "enter the alders";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a dangling <catkin> on the alder branch"), KeywordInContext.Parse("the rough grey <bark> of the alder"), KeywordInContext.Parse("some <nitrogen>-rich soil beside the roots"), KeywordInContext.Parse("a damp alder <grove> ahead") };
    
    private static readonly string[] Moods = { "damp", "grey", "quiet", "moist", "cool", "shadowy", "nitrogen-rich", "serene" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} alder grove";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"walking through a {mood} alder grove";
    }
    
    public sealed class AlderCone : Item
    {
        public override string ItemId => "alder_cone";
        public override string DisplayName => "Alder Cone";
        public override string Description => "A small woody cone from an alder tree";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a small woody <strobilus> from the alder"), KeywordInContext.Parse("some tiny <seed>s inside the cone"), KeywordInContext.Parse("the rough <bark> crumbling at the edges") };
    }
    
    public sealed class AlderCatkin : Item
    {
        public override string ItemId => "alder_grove_catkin";
        public override string DisplayName => "Alder Catkin";
        public override string Description => "A dangling catkin from an alder branch";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some yellow <pollen> dusting the fingers"), KeywordInContext.Parse("a pendulous <ament> hanging from the branch"), KeywordInContext.Parse("a small pale <flower> at the tip") };
    }
}
