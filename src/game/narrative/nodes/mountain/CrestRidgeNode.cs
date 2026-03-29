using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Nodes.Mountain;

public class CrestRidgeNode : PyramidalFeatureNode
{
    public override int MinAltitude => 0;
    public override int MaxAltitude => 1;
    public override bool IsBottomNode => false;
    public override Type PairedNodeType => typeof(CrestShoulderNode);
    
    public override string NodeId => "crest_ridge";
    public override string ContextDescription => "standing on the summit crest ridge";
    public override string TransitionDescription => "ascend to the crest ridge";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the bare <summit> open to the sky"), KeywordInContext.Parse("the knife-edge <crest> of the ridge"), KeywordInContext.Parse("a cutting <wind> howling across the top"), KeywordInContext.Parse("the exposed <peak> above everything") };
    
    private static readonly string[] Moods = { "windswept", "exposed", "highest", "razor-sharp" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} crest ridge";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} crest ridge";
    }
    
    public override List<Item> GetItems() => new() { new FrostShatter(), new RidgePolishedStone(), new SummitCairn() };

    public sealed class FrostShatter : Item
    {
        public override string ItemId => "crest_ridge_frost_shatter";
        public override string DisplayName => "Frost Shatter";
        public override string Description => "Ice-fractured rock fragments at the summit";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a sharp <gelifract> split by repeated freezing"), KeywordInContext.Parse("a thin sheet of <ice> glazing the rock"), KeywordInContext.Parse("a clean <fracture> through the summit stone") };
    }
    
    public sealed class RidgePolishedStone : Item
    {
        public override string ItemId => "ridge_polished_stone";
        public override string DisplayName => "Ridge-Polished Stone";
        public override string Description => "Smooth stone shaped by constant wind";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a rounded <cobble> worn by the ridge wind"), KeywordInContext.Parse("a high <polish> on the windward face"), KeywordInContext.Parse("the constant <wind> that shaped the stone") };
    }
    
    public sealed class SummitCairn : Item
    {
        public override string ItemId => "crest_ridge_summit_cairn";
        public override string DisplayName => "Summit Cairn";
        public override string Description => "Stacked stones marking the highest point";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a stone <waymarker> at the highest point"), KeywordInContext.Parse("a small <monument> of stacked rock"), KeywordInContext.Parse("a crude <marker> left by those before") };
    }
}
