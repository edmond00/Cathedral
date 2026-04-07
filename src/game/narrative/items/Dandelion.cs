using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Dandelion : Item
{
    public override string ItemId => "dandelion";
    public override string DisplayName => "Dandelion";
    public override string Description => "A dandelion in seed, its white globe ready to scatter";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a dandelion <clock> trembling in the air"),
        KeywordInContext.Parse("the white <seed>s ready to drift on the wind"),
    };
}
