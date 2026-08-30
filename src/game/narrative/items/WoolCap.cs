using System.Collections.Generic;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative.Items;

public sealed class WoolCap : WearableItem
{
    public override string ItemId           => "wool_cap";
    public override string DisplayName      => "Wool Cap";
    public override string Description      => "A plain knitted wool cap, lumpen and matted with wear";
    public override WearSlot Slot           => WearSlot.Headgear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override int PriceReference => 10;

    // Knitted wool turns rain, not blades. Its worth is that it marks you as one of the village.
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Peasant };
}
