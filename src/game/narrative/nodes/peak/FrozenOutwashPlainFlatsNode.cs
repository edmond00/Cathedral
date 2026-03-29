using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative.Nodes.Mountain;

namespace Cathedral.Game.Narrative.Nodes.Peak;

public class FrozenOutwashPlainFlatsNode : PyramidalFeatureNode
{
    public override int MinAltitude => 9;
    public override int MaxAltitude => 10;
    public override bool IsBottomNode => true;
    public override Type PairedNodeType => typeof(FrozenOutwashPlainMarginNode);
    
    public override string NodeId => "frozen_outwash_plain_flats";
    public override string ContextDescription => "standing on the frozen outwash plain flats";
    public override string TransitionDescription => "descend to the frozen outwash plain flats";
    public override bool IsEntryNode => false;
    
    public override List<KeywordInContext> NodeKeywordsInContext => new() { KeywordInContext.Parse("the frozen glacial <outwash> spreading flat"), KeywordInContext.Parse("the vast <plain> of ice-covered sediment"), KeywordInContext.Parse("a clear sheet of <ice> cracking underfoot"), KeywordInContext.Parse("the utter <barrenness> of the frozen flats") };
    
    private static readonly string[] Moods = { "expansive", "flat", "barren", "open" };
    
    public override string GenerateNeutralDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        return $"{Moods[rng.Next(Moods.Length)]} frozen flats";
    }

    public override string GenerateEnrichedContextDescription(int locationId = 0)
    {
        var rng = new Random(locationId);
        var mood = Moods[rng.Next(Moods.Length)];
        return $"standing on a {mood} frozen flats";
    }
    
    public override List<Item> GetItems() => new() { new OutwashGravel(), new OutwashClay() };

    public sealed class OutwashGravel : Item
    {
        public override string ItemId => "frozen_outwash_plain_flats_outwash_gravel";
        public override string DisplayName => "Outwash Gravel";
        public override string Description => "Glacial outwash gravel collectible from the flats";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a rough glacial <clast> embedded in the ice"), KeywordInContext.Parse("the wide flat <sandur> of glacial outwash"), KeywordInContext.Parse("the layered <sediment> beneath the frozen crust") };
    }
    
    public sealed class OutwashClay : Item
    {
        public override string ItemId => "frozen_outwash_plain_flats_outwash_clay";
        public override string DisplayName => "Outwash Clay";
        public override string Description => "Fine glacial clay collectible from the flats";
        public override List<KeywordInContext> OutcomeKeywordsInContext => new() { KeywordInContext.Parse("a pale <glaciomarine> deposit in the flats"), KeywordInContext.Parse("the frozen surface of the <sandur> plain"), KeywordInContext.Parse("a fine <mineral> powder in the glacial clay") };
    }
}
