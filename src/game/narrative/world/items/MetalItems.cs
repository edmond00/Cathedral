using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class IronOre : MetalItem
{
    public override string ItemId      => "iron_ore";
    public override string DisplayName => "Iron Ore";
    public override string Description => "A heavy lump of red-brown iron ore, flecked with darker stone";
    public override WeightClass Weight => WeightClass.Medium;
}

public sealed class IronBar : MetalItem
{
    public override string ItemId      => "iron_bar";
    public override string DisplayName => "Iron Bar";
    public override string Description => "A short rough-cast bar of smelted iron, hammer-marks along its sides";
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass    Weight => WeightClass.Heavy;
}

public sealed class Coal : MetalItem
{
    public override string ItemId      => "coal";
    public override string DisplayName => "Coal";
    public override string Description => "A black knob of coal, glittering where it has fractured";
    public override WeightClass Weight => WeightClass.Light;
}

public sealed class Nail : MetalItem
{
    public override string ItemId      => "nail";
    public override string DisplayName => "Nail";
    public override string Description => "A handful of square-cut iron nails";
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork };
    public override int PriceReference => 3;
}

public sealed class IronHoop : MetalItem
{
    public override string ItemId      => "iron_hoop";
    public override string DisplayName => "Iron Hoop";
    public override string Description => "A flat iron band shaped to bind a barrel";
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass    Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork };
    public override int PriceReference => 5;
}
