using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Flour : Item
{
    public override string ItemId      => "flour";
    public override string DisplayName => "Flour";
    public override string Description => "A cloth sack of stone-ground flour, dust pale on its outside";
    public override ItemSize Size => ItemSize.Medium;
    public override float    Weight => 1.5f;
}

public sealed class Ale : Item
{
    public override string ItemId      => "ale";
    public override string DisplayName => "Ale";
    public override string Description => "A clay jug of dark, yeasty ale";
    public override ItemSize Size => ItemSize.Medium;
    public override float    Weight => 1.2f;
    public override List<ItemType> Types => new() { ItemType.Liquid };
}

public sealed class Mug : Item
{
    public override string ItemId      => "mug";
    public override string DisplayName => "Mug";
    public override string Description => "A heavy clay mug, glaze chipped at the rim";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.3f;
}

public sealed class Barrel : Item
{
    public override string ItemId      => "barrel";
    public override string DisplayName => "Barrel";
    public override string Description => "A small oak barrel bound with iron hoops";
    public override ItemSize Size => ItemSize.Large;
    public override float    Weight => 6.0f;
}

public sealed class Lantern : Item
{
    public override string ItemId      => "lantern";
    public override string DisplayName => "Lantern";
    public override string Description => "A pierced-iron lantern with a stub of tallow candle inside";
    public override ItemSize Size => ItemSize.Medium;
    public override float    Weight => 0.7f;
    public override int      UsageLevel => 3;
}

public sealed class Sack : Item
{
    public override string ItemId      => "sack";
    public override string DisplayName => "Sack";
    public override string Description => "A coarse cloth sack, strong-stitched at the seams";
    public override ItemSize Size => ItemSize.Medium;
    public override float    Weight => 0.3f;
}

public sealed class Net : Item
{
    public override string ItemId      => "net";
    public override string DisplayName => "Net";
    public override string Description => "A folded fishing net, weights still tied along its leading edge";
    public override ItemSize Size => ItemSize.Medium;
    public override float    Weight => 1.0f;
}

public sealed class Hook : Item
{
    public override string ItemId      => "hook";
    public override string DisplayName => "Hook";
    public override string Description => "A small barbed iron fish-hook";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.02f;
}

public sealed class FishingLine : Item
{
    public override string ItemId      => "fishing_line";
    public override string DisplayName => "Fishing Line";
    public override string Description => "A coil of waxed linen fishing line";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.1f;
}

public sealed class Basket : Item
{
    public override string ItemId      => "basket";
    public override string DisplayName => "Basket";
    public override string Description => "A woven willow basket with a gappy bottom and a long arched handle";
    public override ItemSize Size => ItemSize.Medium;
    public override float    Weight => 0.5f;
}
