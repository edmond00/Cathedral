using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Cheese : Item
{
    public override string ItemId      => "cheese";
    public override string DisplayName => "Cheese";
    public override string Description => "A wedge of aged yellow cheese, firm-rinded and sharp-smelling";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a wedge of yellow <cheese> wrapped in cloth"),
        KeywordInContext.Parse("the sharp <rind> of an aged farmhouse cheese"),
    };
}
