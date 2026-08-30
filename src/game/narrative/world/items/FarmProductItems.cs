using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Milk : ConsumableItem
{
    public override string ItemId      => "milk";
    public override string DisplayName => "Milk";
    public override string Description => "A wooden pail of fresh, faintly warm milk";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override int PriceReference => 4;
    public override WeightClass Weight => WeightClass.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Drink;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SerumHumor>(45).Add<FatHumor>(30).Add<AquaHumor>(25);
}

/// <summary>Food, and now edible: as a plain item it could be bought and sold but never eaten.</summary>
public sealed class Butter : ConsumableItem
{
    public override string ItemId      => "butter";
    public override string DisplayName => "Butter";
    public override string Article     => "some";
    public override string Description => "A pale block of fresh butter wrapped in a leaf";
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 8;

    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Rich;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(70).Add<SerumHumor>(20).Add<SaltHumor>(10);
}
