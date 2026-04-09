using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class WoolCap : Item
{
    public override string ItemId           => "wool_cap";
    public override string DisplayName      => "Wool Cap";
    public override string Description      => "A plain knitted wool cap, lumpen and matted with wear";
    public override List<ItemType> Types    => new() { ItemType.Headgear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Headgear;
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a lumpen wool <cap> hanging from a nail"),
        KeywordInContext.Parse("the matted <knit> of a long-worn cap"),
    };
}
