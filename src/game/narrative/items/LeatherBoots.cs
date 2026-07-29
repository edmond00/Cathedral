using System.Collections.Generic;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative.Items;

public sealed class LeatherBoots : WearableItem
{
    public override string ItemId           => "leather_boots";
    public override string DisplayName      => "Leather Boots";
    public override string Description      => "A pair of heavy ankle boots, calf leather worn pale at the toes";
    public override ItemSize Size           => ItemSize.Medium;
    public override WearSlot Slot           => WearSlot.Footwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override int PriceReference => 20;

    // Thick calf leather over the ankle — the one piece of ordinary dress that genuinely turns a blow.
    public override int DefenseDice => 1;
    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Peasant, SocialCategory.Military };
}
