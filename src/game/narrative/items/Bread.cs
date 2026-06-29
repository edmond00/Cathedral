using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Bread : ConsumableItem
{
    public override string ItemId      => "bread";
    public override string DisplayName => "Bread";
    public override string Article     => "some";
    public override string Description => "A round dark-rye loaf, heavy and cracked on top";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Rich;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(55).Add<CalxHumor>(18).Add<SaltHumor>(15).Add<FatHumor>(12);
}
