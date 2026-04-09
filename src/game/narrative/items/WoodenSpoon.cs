using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class WoodenSpoon : Item
{
    public override string ItemId      => "wooden_spoon";
    public override string DisplayName => "Wooden Spoon";
    public override string Description => "A long-handled wooden spoon, darkened with use and faintly scorched";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a long wooden <spoon> darkened by the cooking fire"),
        KeywordInContext.Parse("the scorched <handle> of a well-used cooking spoon"),
    };
}
