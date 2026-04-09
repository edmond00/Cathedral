using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Salt : Item
{
    public override string ItemId      => "salt";
    public override string DisplayName => "Salt";
    public override string Description => "A small cloth parcel of coarse grey salt, damp at the edges";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a cloth parcel of coarse grey <salt> on the shelf"),
        KeywordInContext.Parse("the grey <crystal>s of coarse salt spilling from the cloth"),
    };
}
