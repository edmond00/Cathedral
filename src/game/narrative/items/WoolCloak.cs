using System.Collections.Generic;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative.Items;

public sealed class WoolCloak : WearableItem
{
    public override string ItemId           => "wool_cloak";
    public override string DisplayName      => "Wool Cloak";
    public override string Description      => "A heavy earth-brown wool cloak, weather-stained and well-worn";
    public override ItemSize Size           => ItemSize.Large;
    public override WearSlot Slot           => WearSlot.Outerwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override int PriceReference => 20;

    // Heavy enough to soften a glancing blow — but it is a blanket with a clasp, not armour.
    public override int DefenseDice => 1;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Peasant };
}
