using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class BoarTusk : AnimalProductItem
{
    public override string ItemId      => "boar_tusk";
    public override string DisplayName => "Boar Tusk";
    public override string Description => "A curved yellow tusk pulled from a boar's jaw";
    public override List<ItemTag> Tags => new() { ItemTag.Pelt };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 8;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass    Weight => WeightClass.Light;
}

public sealed class WolfPelt : AnimalProductItem
{
    public override string ItemId      => "wolf_pelt";
    public override string DisplayName => "Wolf Pelt";
    public override string Description => "The grey-furred pelt of a wolf, still fresh and strong-smelling";
    public override List<ItemTag> Tags => new() { ItemTag.Pelt };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 20;
    public override ItemSize Size => ItemSize.Large;
    public override WeightClass    Weight => WeightClass.Heavy;
}

public sealed class DeerHide : AnimalProductItem
{
    public override string ItemId      => "deer_hide";
    public override string DisplayName => "Deer Hide";
    public override string Description => "A folded brown deer hide, soft-haired and pliable";
    public override List<ItemTag> Tags => new() { ItemTag.Pelt };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 18;
    public override ItemSize Size => ItemSize.Large;
    public override WeightClass    Weight => WeightClass.Medium;
}

public sealed class GoatHide : AnimalProductItem
{
    public override string ItemId      => "goat_hide";
    public override string DisplayName => "Goat Hide";
    public override string Description => "A coarse goat hide, hair pale and oily";
    public override List<ItemTag> Tags => new() { ItemTag.Pelt };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 14;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass    Weight => WeightClass.Medium;
}

public sealed class LynxPelt : AnimalProductItem
{
    public override string ItemId      => "lynx_pelt";
    public override string DisplayName => "Lynx Pelt";
    public override string Description => "A spotted lynx pelt, prized and rare";
    public override List<ItemTag> Tags => new() { ItemTag.Pelt };
    public override int PriceReference => 20;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass    Weight => WeightClass.Medium;
}

public sealed class SealPelt : AnimalProductItem
{
    public override string ItemId      => "seal_pelt";
    public override string DisplayName => "Seal Pelt";
    public override string Description => "A sleek dark seal pelt, oily and supple";
    public override List<ItemTag> Tags => new() { ItemTag.Pelt };
    public override int PriceReference => 20;
    public override ItemSize Size => ItemSize.Large;
    public override WeightClass    Weight => WeightClass.Heavy;
}

public sealed class EagleFeather : AnimalProductItem
{
    public override string ItemId      => "eagle_feather";
    public override string DisplayName => "Eagle Feather";
    public override string Description => "A long brown-banded eagle feather";
    public override List<ItemTag> Tags => new() { ItemTag.Pelt };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 6;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass    Weight => WeightClass.Insignificant;
}

public sealed class Feather : AnimalProductItem
{
    public override string ItemId      => "feather";
    public override string DisplayName => "Feather";
    public override string Description => "A small bird feather, soft-vaned";
    public override List<ItemTag> Tags => new() { ItemTag.Pelt };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 2;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass    Weight => WeightClass.Insignificant;
}
