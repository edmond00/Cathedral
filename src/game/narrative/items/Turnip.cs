using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Turnip : ConsumableItem
{
    public override string ItemId      => "turnip";
    public override string DisplayName => "Turnip";
    public override string Description => "A knobbed purple-white root, still clodded with earth";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(50).Add<PulpHumor>(30).Add<VaporHumor>(20);
}
