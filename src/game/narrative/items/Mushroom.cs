using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Mushroom : ConsumableItem
{
    public override string ItemId => "mushroom";
    public override string DisplayName => "Mushroom";
    public override string Description => "A pale mushroom growing in shade at the base of a rock";
    public override List<ItemTag> Tags => new() { ItemTag.Forage };
    public override int PriceReference => 3;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FungiHumor>(60).Add<FiberHumor>(25).Add<PhlegmHumor>(15);
}
