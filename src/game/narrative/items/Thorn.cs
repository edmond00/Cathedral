using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Thorn : Item
{
    public override string ItemId => "thorn";
    public override string DisplayName => "Thorn";
    public override string Description => "A hard, curved thorn broken from a branch";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a hard curved <thorn> broken from the wood"),
        KeywordInContext.Parse("the sharp <point> of a detached thorn"),
    };
}
