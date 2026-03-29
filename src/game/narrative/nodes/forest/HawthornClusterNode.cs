using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Hawthorn Cluster - A group of thorny hawthorn bushes.
/// </summary>
public class HawthornClusterNode : NarrationNode
{
    public override string NodeId => "hawthorn_cluster";
    public override string ContextDescription => "carefully examining the hawthorns";
    public override string TransitionDescription => "approach the hawthorns";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a long sharp <thorn> on the main stem"), KeywordInContext.Parse("some small white <flower>s in tight clusters"), KeywordInContext.Parse("a red haw <berry> among the leaves"), KeywordInContext.Parse("the impenetrable <hawthorn> hedge ahead") };
    
    private static readonly string[] Moods = { "thorny", "defensive", "prickly", "flowering", "fragrant", "clustered", "tangled", "guarded" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} hawthorn cluster";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"examining a {mood} hawthorn cluster";
    }
    
    public override List<Item> GetItems() => new() { new Haw(), new HawthornThorn() };

    public sealed class Haw : Item
    {
        public override string ItemId => "hawthorn_berry";
        public override string DisplayName => "Haw";
        public override string Description => "A red hawthorn berry, slightly mealy";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a mealy red <berry> from the hawthorn"), KeywordInContext.Parse("the hard <seed> inside the haw fruit") };
    }
    
    public sealed class HawthornThorn : Item
    {
        public override string ItemId => "hawthorn_cluster_thorn";
        public override string DisplayName => "Hawthorn Thorn";
        public override string Description => "A long, sharp thorn from a hawthorn branch";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a sharp <barb> snapped from the branch"), KeywordInContext.Parse("a hard straight <spine> from the hawthorn stem") };
    }
}
