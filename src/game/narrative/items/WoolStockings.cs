using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class WoolStockings : Item
{
    public override string ItemId           => "wool_stockings";
    public override string DisplayName      => "Wool Stockings";
    public override string Description      => "A pair of hand-knitted wool stockings, darned at the heel";
    public override List<ItemType> Types    => new() { ItemType.Legwear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Legwear;
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a pair of knitted wool <stockings> rolled on the shelf"),
        KeywordInContext.Parse("the darned <heel> of a well-worn wool stocking"),
    };
}
