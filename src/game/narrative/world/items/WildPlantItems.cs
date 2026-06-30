using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Nettle : ConsumableItem
{
    public override string ItemId      => "nettle";
    public override string DisplayName => "Nettle";
    public override string Description => "A stinging nettle stem, leaves bristling with fine hairs";
    public override List<ItemTag> Tags => new() { ItemTag.Forage };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 2;
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.05f;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(50).Add<YellowBileHumor>(30).Add<VaporHumor>(20);
}

public sealed class Fern : ConsumableItem
{
    public override string ItemId      => "fern";
    public override string DisplayName => "Fern";
    public override string Description => "A curled green fern frond, soft underside paler than its top";
    public override List<ItemTag> Tags => new() { ItemTag.Forage };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 2;
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.05f;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PulpHumor>(45).Add<FungiHumor>(30).Add<FiberHumor>(25);
}

public sealed class Ivy : Item
{
    public override string ItemId      => "ivy";
    public override string DisplayName => "Ivy";
    public override string Description => "A trailing length of ivy, leaves leathery and dark";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.1f;
    public override List<ItemTag> Tags => new() { ItemTag.Forage };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 1;
}

public sealed class Bramble : ConsumableItem
{
    public override string ItemId      => "bramble";
    public override string DisplayName => "Bramble";
    public override string Description => "A thorny bramble cane, snagging on cloth";
    public override List<ItemTag> Tags => new() { ItemTag.Forage };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 2;
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.1f;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PulpHumor>(45).Add<FumeHumor>(30).Add<FiberHumor>(25);
}

public sealed class Reed : ConsumableItem
{
    public override string ItemId      => "reed";
    public override string DisplayName => "Reed";
    public override string Description => "A cluster of tall hollow reeds, papery at the edges";
    public override List<ItemTag> Tags => new() { ItemTag.Forage };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 2;
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.1f;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(55).Add<SaltHumor>(25).Add<PhlegmHumor>(20);
}

public sealed class Watercress : ConsumableItem
{
    public override string ItemId      => "watercress";
    public override string DisplayName => "Watercress";
    public override string Description => "A wet handful of watercress, peppery-smelling";
    public override List<ItemTag> Tags => new() { ItemTag.Forage };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 2;
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.1f;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(45).Add<VaporHumor>(35).Add<PulpHumor>(20);
}
