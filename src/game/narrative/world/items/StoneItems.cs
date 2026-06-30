using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Clay : ConsumableItem
{
    public override string ItemId      => "clay";
    public override string DisplayName => "Clay";
    public override string Description => "A wet lump of grey-brown clay, cool and dense in the hand";
    public override List<ItemTag> Tags => new() { ItemTag.Mineral };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 6;
    public override float Weight => 0.6f;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BlackBileHumor>(50).Add<PhlegmHumor>(30).Add<SaltHumor>(20);
}

public sealed class Lichen : ConsumableItem
{
    public override string ItemId      => "lichen";
    public override string DisplayName => "Lichen";
    public override string Description => "A papery crust of grey-green lichen prised from rock";
    public override List<ItemTag> Tags => new() { ItemTag.Mineral };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 4;
    public override float Weight => 0.05f;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(45).Add<FungiHumor>(30).Add<YellowBileHumor>(25);
}
