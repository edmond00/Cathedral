using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

/// <summary>Sealed with wax, so it carries what is in it — a vessel rather than cookware.</summary>
public sealed class ClayPot : ContainerItem
{
    public override string ItemId      => "clay_pot";
    public override string DisplayName => "Clay Pot";
    public override string Description => "A squat clay cooking pot, fire-blackened and sealed with a plug of wax";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass Weight       => WeightClass.Light;
    public override ContainerKind Kind => ContainerKind.Vessel;
    public override int ContentSlots   => 6;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 8;
}
