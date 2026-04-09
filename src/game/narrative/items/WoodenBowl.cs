using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class WoodenBowl : Item
{
    public override string ItemId      => "wooden_bowl";
    public override string DisplayName => "Wooden Bowl";
    public override string Description => "A turned wooden bowl, smooth inside and darkened with years of use";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a turned wooden <bowl> dark with years of use"),
        KeywordInContext.Parse("the worn <rim> of a well-handled eating bowl"),
    };
}
