using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class DriedPeas : Item
{
    public override string ItemId      => "dried_peas";
    public override string DisplayName => "Dried Peas";
    public override string Description => "A small sack of dried peas, hard and wrinkled, rattling loosely";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a loose cloth <sack> of dried rattling peas"),
        KeywordInContext.Parse("the dry <rattle> of hard peas in a tied cloth bag"),
    };
}
