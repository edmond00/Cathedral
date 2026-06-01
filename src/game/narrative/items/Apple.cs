using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Apple : ConsumableItem
{
    public override string ItemId => "apple";
    public override string DisplayName => "Apple";
    public override string Description => "A ripe apple, red-green and faintly bruised";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new SugarHumor(), new FiberHumor(), new PulpHumor() }
        .GetRange(0, PickHumorCount(rng));
}
