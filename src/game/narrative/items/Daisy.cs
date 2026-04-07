using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Daisy : Item
{
    public override string ItemId => "daisy";
    public override string DisplayName => "Daisy";
    public override string Description => "A common white daisy with a yellow centre";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a white <daisy> with a bright yellow eye"),
        KeywordInContext.Parse("the slim <petal>s of a plucked daisy"),
    };
}
