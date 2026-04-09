using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Bread : Item
{
    public override string ItemId      => "bread";
    public override string DisplayName => "Bread";
    public override string Description => "A round dark-rye loaf, heavy and cracked on top";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a round dark <loaf> of rye bread on the shelf"),
        KeywordInContext.Parse("the cracked <crust> of a fresh-baked farmhouse loaf"),
    };
}
