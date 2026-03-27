using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Forest;

/// <summary>
/// Trunk Cluster - Multiple tree trunks growing very close together.
/// Associated with: DeepCanopy
/// </summary>
public class TrunkClusterNode : NarrationNode
{
    public override string NodeId => "trunk_cluster";
    public override string ContextDescription => "navigating the trunk cluster";
    public override string TransitionDescription => "weave through the trunks";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a broad grey <trunk> pressing close on either side"), KeywordInContext.Parse("a living <column> of bark rising overhead"), KeywordInContext.Parse("the tight <cluster> of boles growing almost as one"), KeywordInContext.Parse("a sense of <density> where no light penetrates") };
    
    private static readonly string[] Moods = { "crowded", "tight", "clustered", "grouped", "packed", "close-growing", "compressed", "dense" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        
        return $"{mood} trunk cluster";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"navigating a {mood} trunk cluster";
    }
    
    public sealed class InnerBark : Item
    {
        public override string ItemId => "inner_bark_strip";
        public override string DisplayName => "Inner Bark Strip";
        public override string Description => "A pale strip of inner bark from tight-growing trunks";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("the soft pale <phloem> beneath the outer bark"), KeywordInContext.Parse("the living <cambium> layer just under the surface"), KeywordInContext.Parse("a strip of tough woody <fiber> from the inner bark") };
    }
    
    public sealed class BarkShaving : Item
    {
        public override string ItemId => "trunk_cluster_bark_shaving";
        public override string DisplayName => "Bark Shaving";
        public override string Description => "Thin bark shavings rubbed off by close trunks";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some thin flakes of outer <cortex> rubbed from the bark"), KeywordInContext.Parse("a curl of dry bark <tinder> from the close-packed trunks"), KeywordInContext.Parse("the marks of <friction> where trunks have worn each other smooth") };
    }
}
