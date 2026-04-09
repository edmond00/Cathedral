using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Onion : Item
{
    public override string ItemId      => "onion";
    public override string DisplayName => "Onion";
    public override string Description => "A dried onion with papery brown skin, pungent and firm";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a papery brown <onion> hanging from a string"),
        KeywordInContext.Parse("the sharp <smell> of dried onion in the air"),
    };
}
