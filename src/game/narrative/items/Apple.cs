using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Apple : ConsumableItem
{
    public override string ItemId => "apple";
    public override string DisplayName => "Apple";
    public override string Description => "A ripe apple, red-green and faintly bruised";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PulpHumor>(70).Add<SugarHumor>(25).Add<FiberHumor>(5);
}
