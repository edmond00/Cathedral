using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Cabbage : Item
{
    public override string ItemId      => "cabbage";
    public override string DisplayName => "Cabbage";
    public override string Description => "A firm pale-green head of cabbage, its outer leaves limp";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a pale green <cabbage> head sitting in the barrel"),
        KeywordInContext.Parse("the limp outer <leaf> of a stored cabbage"),
    };
}
