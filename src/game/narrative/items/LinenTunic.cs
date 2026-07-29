using System.Collections.Generic;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative.Items;

public sealed class LinenTunic : WearableItem
{
    public override string ItemId           => "linen_tunic";
    public override string DisplayName      => "Linen Tunic";
    public override string Description      => "A coarse off-white linen tunic, rough at the collar and well-mended";
    public override ItemSize Size           => ItemSize.Medium;
    public override WearSlot Slot           => WearSlot.Bodywear;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override int PriceReference => 10;

    // Mended rather than ragged: it reads as a working man who keeps himself decent.
    public override IReadOnlyList<SocialCategory> DialogueAppeal =>
        new[] { SocialCategory.Peasant, SocialCategory.Bourgeois };
}
