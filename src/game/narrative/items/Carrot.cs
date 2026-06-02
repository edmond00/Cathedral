using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Carrot : ConsumableItem
{
    public override string ItemId      => "carrot";
    public override string DisplayName => "Carrot";
    public override string Description => "A long orange root with feathery green tops, fresh-pulled";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(45).Add<PulpHumor>(30).Add<SugarHumor>(25);
}
