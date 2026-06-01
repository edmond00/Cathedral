using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Carrot : ConsumableItem
{
    public override string ItemId      => "carrot";
    public override string DisplayName => "Carrot";
    public override string Description => "A long orange root with feathery green tops, fresh-pulled";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new FiberHumor(), new SugarHumor(), new VaporHumor() }
        .GetRange(0, PickHumorCount(rng));
}
