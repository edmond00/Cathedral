using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Rock : Item
{
    public override string ItemId => "rock";
    public override string DisplayName => "Rock";
    public override string Description => "A fist-sized rock broken from a boulder face";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a fist-sized <rock> broken from the face"),
        KeywordInContext.Parse("the rough <grain> of a loose field stone"),
    };
}
