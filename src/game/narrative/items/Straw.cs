using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Straw : Item
{
    public override string ItemId      => "straw";
    public override string DisplayName => "Straw";
    public override string Description => "A handful of dry golden straw stalks, hollow and brittle";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a loose <straw> scattered across the floor"),
        KeywordInContext.Parse("the brittle <stem> of dry cereal straw"),
    };
}
