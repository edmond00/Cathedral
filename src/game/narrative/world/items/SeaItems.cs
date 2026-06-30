using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Herring : SeaFoodItem
{
    public override string ItemId      => "herring";
    public override string DisplayName => "Herring";
    public override string Description => "A silver-flanked herring, eyes still bright";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(40).Add<SaltHumor>(35).Add<FatHumor>(25);
}

public sealed class Cod : SeaFoodItem
{
    public override string ItemId      => "cod";
    public override string DisplayName => "Cod";
    public override string Description => "A fat cod, mottled grey-green along its back";
    public override ItemSize Size => ItemSize.Medium;
    public override float    Weight => 1.0f;
    protected override HumorRichness Richness => HumorRichness.Rich;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(45).Add<FatHumor>(30).Add<SaltHumor>(25);
}

public sealed class Mackerel : SeaFoodItem
{
    public override string ItemId      => "mackerel";
    public override string DisplayName => "Mackerel";
    public override string Description => "A streamlined mackerel banded with iridescent green-blue";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(40).Add<FatHumor>(30).Add<SaltHumor>(30);
}

public sealed class Crab : SeaFoodItem
{
    public override string ItemId      => "crab";
    public override string DisplayName => "Crab";
    public override string Description => "A scuttling brown crab, claws still snapping";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SaltHumor>(35).Add<CalxHumor>(30).Add<FatHumor>(20).Add<BloodHumor>(15);
}

public sealed class Mussel : SeaFoodItem
{
    public override string ItemId      => "mussel";
    public override string DisplayName => "Mussel";
    public override string Description => "A fistful of black-shelled mussels clamped tight";
    protected override HumorRichness Richness => HumorRichness.Modest;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SaltHumor>(40).Add<BloodHumor>(30).Add<CalxHumor>(30);
}

public sealed class Shell : Item
{
    public override string ItemId      => "shell";
    public override string DisplayName => "Shell";
    public override string Description => "A pearly fragment of seashell, edges worn smooth by tide";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.05f;
}

public sealed class Seaweed : ConsumableItem
{
    public override string ItemId      => "seaweed";
    public override string DisplayName => "Seaweed";
    public override string Description => "A heavy strand of brown seaweed, cool and slick";
    public override List<ItemTag> Tags => new() { ItemTag.Fish };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 2;
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.2f;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SaltHumor>(50).Add<FiberHumor>(35).Add<PhlegmHumor>(15);
}

public sealed class Driftwood : Item
{
    public override string ItemId      => "driftwood";
    public override string DisplayName => "Driftwood";
    public override string Description => "A water-pale piece of driftwood, salt-cracked";
    public override ItemSize Size => ItemSize.Medium;
    public override float    Weight => 0.8f;
}

public sealed class RopeFragment : Item
{
    public override string ItemId      => "rope_fragment";
    public override string DisplayName => "Rope Fragment";
    public override string Description => "A frayed length of tarred rope cut by a tide-rock";
    public override ItemSize Size => ItemSize.Small;
    public override float    Weight => 0.2f;
}
