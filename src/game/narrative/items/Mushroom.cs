using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Mushroom : Item
{
    public override string ItemId => "mushroom";
    public override string DisplayName => "Mushroom";
    public override string Description => "A pale mushroom growing in shade at the base of a rock";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a pale <mushroom> growing in the shade"),
        KeywordInContext.Parse("the smooth <cap> of a rock-base mushroom"),
    };
}
