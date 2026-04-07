using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class WildBerry : Item
{
    public override string ItemId => "wild_berry";
    public override string DisplayName => "Wild Berry";
    public override string Description => "A small dark berry of uncertain edibility";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a cluster of dark <berry>s on a stem"),
        KeywordInContext.Parse("the small waxy <globe> of a wild berry"),
    };
}
