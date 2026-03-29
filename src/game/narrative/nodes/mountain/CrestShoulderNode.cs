using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class CrestShoulderNode : PyramidalFeatureNode
{
    public override int MinAltitude => 0;
    public override int MaxAltitude => 1;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(CrestRidgeNode);
    
    public override string NodeId => "crest_shoulder";
    public override string ContextDescription => "on the summit crest shoulder";
    public override string TransitionDescription => "descend to the crest shoulder";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the broad <shoulder> falling from the crest"), KeywordInContext.Parse("a steep <slope> dropping into the void"), KeywordInContext.Parse("a fierce <wind> battering the exposed rock"), KeywordInContext.Parse("the absolute <barrenness> of the high stone") };
    
    private static readonly string[] Moods = { "steep", "exposed", "wind-battered", "barren" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} crest shoulder";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"on a {mood} crest shoulder";
    }
    
    public override List<Item> GetItems() => new() { new LooseScree(), new AlpineQuartz() };

    public sealed class LooseScree : Item
    {
        public override string ItemId => "crest_shoulder_loose_scree";
        public override string DisplayName => "Loose Scree";
        public override string Description => "Unstable rock debris on the shoulder";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("some loose <talus> sliding from the shoulder"), KeywordInContext.Parse("a scatter of coarse <gravel> across the rock"), KeywordInContext.Parse("a creeping <instability> in the loose debris") };
    }
    
    public sealed class AlpineQuartz : Item
    {
        public override string ItemId => "crest_shoulder_alpine_quartz";
        public override string DisplayName => "Alpine Quartz";
        public override string Description => "Clear quartz crystal collectible from the shoulder";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a veined <silicate> embedded in the stone"), KeywordInContext.Parse("a clear <crystal> glinting in the cold light"), KeywordInContext.Parse("a rare <mineral> exposed by frost action") };
    }
}
