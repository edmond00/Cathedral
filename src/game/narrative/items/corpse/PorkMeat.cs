using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class PorkMeat : Item
{
    public override string ItemId      => "pork_meat";
    public override string DisplayName => "Pork Meat";
    public override string Description => "A heavy cut of raw pork, marbled with fat and still bleeding";
    public override float Weight       => 0.8f;
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a thick cut of raw <pork> from the flank"),
        KeywordInContext.Parse("the marbled red <flesh> of a freshly slaughtered pig"),
    };
}
