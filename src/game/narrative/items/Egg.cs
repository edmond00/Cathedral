using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Egg : Item
{
    public override string ItemId      => "egg";
    public override string DisplayName => "Egg";
    public override string Description => "A brown hen's egg, warm and faintly spotted";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a brown <egg> nestled in a scrape of straw"),
        KeywordInContext.Parse("the smooth <shell> of a fresh hen's egg"),
    };
}
