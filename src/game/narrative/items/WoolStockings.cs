using System.Collections.Generic;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative.Items;

public sealed class WoolStockings : WearableItem
{
    public override string ItemId           => "wool_stockings";
    public override string DisplayName      => "Wool Stockings";
    public override string Description      => "A pair of hand-knitted wool stockings, darned at the heel";
    public override WearSlot Slot           => WearSlot.Legwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override int PriceReference => 10;

    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Peasant };
}
