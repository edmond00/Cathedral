using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Moss : Item
{
    public override string ItemId => "moss";
    public override string DisplayName => "Moss";
    public override string Description => "A damp clump of dark green moss";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a damp clump of dark green <moss>"),
        KeywordInContext.Parse("the wet <cushion> of moss from the shaded face"),
    };
}
