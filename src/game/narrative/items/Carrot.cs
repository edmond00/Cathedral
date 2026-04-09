using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Carrot : Item
{
    public override string ItemId      => "carrot";
    public override string DisplayName => "Carrot";
    public override string Description => "A long orange root with feathery green tops, fresh-pulled";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a bright orange <carrot> pulled free of dark soil"),
        KeywordInContext.Parse("the feathery green <top> of a freshly pulled carrot"),
    };
}
