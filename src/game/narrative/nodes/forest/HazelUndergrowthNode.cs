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
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a long yellow <catkin> dangling from a stem"), KeywordInContext.Parse("some ripe hazel <nut>s in their leafy husks"), KeywordInContext.Parse("a dense multi-stemmed <bush> filling the space"), KeywordInContext.Parse("a hopeless <tangle> of hazel shoots ahead") };
    
    private static readonly string[] Moods = { "dense", "tangled", "productive", "crowded", "bushy", "nutty", "thriving", "clustered" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} hazel undergrowth";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"searching a {mood} hazel undergrowth";
    }
    
    public override List<Item> GetItems() => new() { new Hazelnut(), new HazelCatkin() };

    public sealed class Hazelnut : Item
    {
        public override string ItemId => "hazelnut";
        public override string DisplayName => "Hazelnut";
        public override string Description => "A ripe hazelnut in its leafy husk";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a ripe brown <nut> in its leafy collar"), KeywordInContext.Parse("a hard smooth <shell> around the kernel"), KeywordInContext.Parse("the green leafy <husk> still attached") };
    }
    
    public sealed class HazelCatkin : Item
    {
        public override string ItemId => "hazel_undergrowth_catkin";
        public override string DisplayName => "Hazel Catkin";
        public override string Description => "A yellow male catkin hanging from a branch";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a long yellow <ament> hanging in the breeze"), KeywordInContext.Parse("some fine yellow <pollen> dusting the fingers") };
    }
}
