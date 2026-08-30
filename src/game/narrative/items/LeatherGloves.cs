using System.Collections.Generic;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative.Items;

public sealed class LeatherGloves : WearableItem
{
    public override string ItemId           => "leather_gloves";
    public override string DisplayName      => "Leather Gloves";
    public override string Description      => "A pair of stiff work gloves in thick undyed leather, cracked at the knuckles";
    public override WearSlot Slot           => WearSlot.Handwear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override int PriceReference => 10;

    // Stiff enough to save a knuckle, and unmistakably the hands of someone who works.
    public override int DefenseDice => 1;
    public override IReadOnlyList<SocialCategory> DialogueAppeal => new[] { SocialCategory.Peasant };
}
