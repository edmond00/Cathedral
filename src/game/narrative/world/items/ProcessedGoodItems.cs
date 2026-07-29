using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Flour : ConsumableItem
{
    public override string ItemId      => "flour";
    public override string DisplayName => "Flour";
    public override string Description => "A cloth sack of stone-ground flour, dust pale on its outside";
    public override List<ItemTag> Tags => new() { ItemTag.Crop, ItemTag.Foodstuff };
    public override int PriceReference => 6;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass    Weight => WeightClass.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(60).Add<CalxHumor>(25).Add<PulpHumor>(15);
}

public sealed class Ale : ConsumableItem
{
    public override string ItemId      => "ale";
    public override string DisplayName => "Ale";
    public override string Description => "A clay jug of dark, yeasty ale";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override int PriceReference => 5;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass    Weight => WeightClass.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Drink;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AlcoholHumor>(50).Add<SugarHumor>(25).Add<FumeHumor>(25);
}

public sealed class Mug : ContainerItem
{
    public override string ItemId      => "mug";
    public override string DisplayName => "Mug";
    public override string Description => "A heavy clay mug, glaze chipped at the rim";
    public override ContainerKind Kind => ContainerKind.Vessel;
    public override int ContentSlots   => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 8;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass    Weight => WeightClass.Light;
}

public sealed class Barrel : ContainerItem
{
    public override string ItemId      => "barrel";
    public override string DisplayName => "Barrel";
    public override string Description => "A small oak barrel bound with iron hoops";
    public override ContainerKind Kind => ContainerKind.Vessel;
    public override int ContentSlots   => 15;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 25;
    public override ItemSize Size => ItemSize.Large;
    public override WeightClass    Weight => WeightClass.Heavy;
}

public sealed class Lantern : Item
{
    public override string ItemId      => "lantern";
    public override string DisplayName => "Lantern";
    public override string Description => "A pierced-iron lantern with a stub of tallow candle inside";
    public override ItemCategory Category => ItemCategory.Tool;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 30;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass    Weight => WeightClass.Light;
    public override int      UsageLevel => 3;
}

public sealed class Sack : ContainerItem
{
    public override string ItemId      => "sack";
    public override string DisplayName => "Sack";
    public override string Description => "A coarse cloth sack, strong-stitched at the seams";
    public override ContainerKind Kind => ContainerKind.Storage;
    public override int ContentSlots   => 12;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 8;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass    Weight => WeightClass.Light;
}

public sealed class Net : Item
{
    public override string ItemId      => "net";
    public override string DisplayName => "Net";
    public override string Description => "A folded fishing net, weights still tied along its leading edge";
    public override ItemCategory Category => ItemCategory.Tool;
    public override int      UsageLevel => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 18;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass    Weight => WeightClass.Medium;
}

public sealed class Hook : Item
{
    public override string ItemId      => "hook";
    public override string DisplayName => "Hook";
    public override string Description => "A small barbed iron fish-hook";
    public override ItemCategory Category => ItemCategory.Tool;
    public override int      UsageLevel => 2;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 8;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass    Weight => WeightClass.Insignificant;
}

public sealed class FishingLine : Item
{
    public override string ItemId      => "fishing_line";
    public override string DisplayName => "Fishing Line";
    public override string Description => "A coil of waxed linen fishing line";
    public override ItemCategory Category => ItemCategory.Tool;
    public override int      UsageLevel => 2;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 8;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass    Weight => WeightClass.Insignificant;
}

public sealed class Basket : ContainerItem
{
    public override string ItemId      => "basket";
    public override string DisplayName => "Basket";
    public override string Description => "A woven willow basket with a gappy bottom and a long arched handle";
    public override ContainerKind Kind => ContainerKind.Storage;
    public override int ContentSlots   => 9;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 10;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass    Weight => WeightClass.Light;
}
