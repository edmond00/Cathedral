using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class DriedPeas : ConsumableItem
{
    public override string ItemId      => "dried_peas";
    public override string DisplayName => "Dried Peas";
    public override string Description => "A small sack of dried peas, hard and wrinkled, rattling loosely";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    public override bool IsHard => true;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(45).Add<PulpHumor>(35).Add<SaltHumor>(20);
}
