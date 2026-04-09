using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class DriedMeat : Item
{
    public override string ItemId      => "dried_meat";
    public override string DisplayName => "Dried Meat";
    public override string Description => "A strip of salted dark meat, hard and leathery, smelling of brine";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a strip of dark dried <meat> hanging from the rafter"),
        KeywordInContext.Parse("the brine <smell> of salt-cured meat in the air"),
    };
}
