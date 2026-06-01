using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Cabbage : ConsumableItem
{
    public override string ItemId      => "cabbage";
    public override string DisplayName => "Cabbage";
    public override string Description => "A firm pale-green head of cabbage, its outer leaves limp";
    public override ItemSize Size => ItemSize.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new FiberHumor(), new VaporHumor(), new PhlegmHumor() }
        .GetRange(0, PickHumorCount(rng));
}
