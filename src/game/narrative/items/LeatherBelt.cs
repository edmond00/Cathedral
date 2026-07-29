using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

/// <summary>
/// A belt does not protect and does not flatter — what it does is carry. Modelled as a small
/// storage container so it satisfies the rule that every wearable must do something: the things
/// hung from it are its whole purpose.
/// </summary>
public sealed class LeatherBelt : WearableContainerItem
{
    public override string ItemId           => "leather_belt";
    public override string DisplayName      => "Leather Belt";
    public override string Description      => "A thick leather belt with a plain iron buckle, creased and darkened";
    public override WearSlot Slot           => WearSlot.BeltGear;
    public override ContainerKind Kind      => ContainerKind.Storage;
    public override int ContentSlots        => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Clothing };
    public override int PriceReference => 10;

    // Creased and darkened with years of wear: a working man's belt, and nothing more than that.
    public override IReadOnlyList<Npc.SocialCategory> DialogueAppeal =>
        new[] { Npc.SocialCategory.Peasant, Npc.SocialCategory.Pauper };
}
