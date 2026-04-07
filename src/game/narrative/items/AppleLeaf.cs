using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class AppleLeaf : Item
{
    public override string ItemId => "apple_leaf";
    public override string DisplayName => "Apple Leaf";
    public override string Description => "A broad waxy leaf from an apple tree";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a broad <leaf> still attached to a short stem"),
        KeywordInContext.Parse("the waxy <surface> of an apple tree leaf"),
    };
}
