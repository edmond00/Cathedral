using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Mushroom : ConsumableItem
{
    public override string ItemId => "mushroom";
    public override string DisplayName => "Mushroom";
    public override string Description => "A pale mushroom growing in shade at the base of a rock";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new FungiHumor(), new FiberHumor(), new EtherHumor() }
        .GetRange(0, PickHumorCount(rng));
}
