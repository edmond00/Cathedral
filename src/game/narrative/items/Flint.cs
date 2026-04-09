using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Flint : Item
{
    public override string ItemId      => "flint";
    public override string DisplayName => "Flint";
    public override string Description => "A sharp-edged flint nodule, one face knapped flat for striking fire";
    public override int    UsageLevel  => 2;
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a grey <flint> nodule with one flat knapped face"),
        KeywordInContext.Parse("the sharp <edge> of a fire-starting flint"),
    };
}
