using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Clover : Item
{
    public override string ItemId => "clover";
    public override string DisplayName => "Clover";
    public override string Description => "A sprig of three-leafed clover with a small pink head";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a sprig of green <clover> with a pink head"),
        KeywordInContext.Parse("the small round <floret> of a clover blossom"),
    };
}
