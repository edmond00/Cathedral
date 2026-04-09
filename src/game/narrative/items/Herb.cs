using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Herb : Item
{
    public override string ItemId      => "herb";
    public override string DisplayName => "Dried Herbs";
    public override string Description => "A bundle of dried culinary herbs, crumbling and faintly fragrant";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a bundle of dried <herbs> tied with thin twine"),
        KeywordInContext.Parse("the faint <fragrance> of dried culinary herbs"),
    };
}
