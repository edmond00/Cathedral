using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

/// <summary>Open-topped: it holds a drink while you sit with it, but will not travel full.</summary>
public sealed class WoodenBowl : ContainerItem
{
    public override string ItemId      => "wooden_bowl";
    public override string DisplayName => "Wooden Bowl";
    public override string Description => "A turned wooden bowl, smooth inside and darkened with years of use";
    public override ContainerKind Kind => ContainerKind.Vessel;
    public override int ContentSlots   => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 6;
}
