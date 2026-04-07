using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Poppy : Item
{
    public override string ItemId => "poppy";
    public override string DisplayName => "Poppy";
    public override string Description => "A vivid red poppy, its petals paper-thin";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a blood-red <poppy> swaying on a thin stem"),
        KeywordInContext.Parse("the fragile <petal> of a common poppy"),
    };
}
