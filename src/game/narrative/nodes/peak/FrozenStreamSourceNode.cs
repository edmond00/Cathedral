using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class FrozenStreamSourceNode : PyramidalFeatureNode
{
    public override int MinAltitude => 5;
    public override int MaxAltitude => 9;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(FrozenStreamChannelNode);
    
    public override string NodeId => "frozen_stream_source";
    public override string ContextDescription => "standing at the frozen stream source";
    public override string TransitionDescription => "reach the frozen stream source";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("a frozen mountain <spring> locked in ice"), KeywordInContext.Parse("a clear sheet of <ice> sealing the source"), KeywordInContext.Parse("the original <source> of the frozen stream"), KeywordInContext.Parse("a slow cold <seep> from beneath the ice") };
    
    private static readonly string[] Moods = { "nascent", "frozen", "pristine", "originating" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} frozen source";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing at a {mood} frozen source";
    }
    
    public override List<Item> GetItems() => new() { new SpringQuartz() };

    public sealed class SpringQuartz : Item
    {
        public override string ItemId => "frozen_stream_source_spring_quartz";
        public override string DisplayName => "Spring Quartz";
        public override string Description => "Clear quartz crystal collectible from the spring";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a veined <silicate> from the spring rock"), KeywordInContext.Parse("a clear <crystal> grown at the water seep"), KeywordInContext.Parse("the slow <seepage> welling beneath the ice") };
    }
}
