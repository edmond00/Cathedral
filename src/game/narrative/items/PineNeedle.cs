using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class PineNeedle : Item
{
    public override string ItemId => "pine_needle";
    public override string DisplayName => "Pine Needles";
    public override string Description => "A small cluster of stiff, sharp pine needles";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a cluster of stiff <needle>s from the pine"),
        KeywordInContext.Parse("the sharp <tip> of a fallen pine needle"),
    };
}
