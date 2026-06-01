using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Bread : ConsumableItem
{
    public override string ItemId      => "bread";
    public override string DisplayName => "Bread";
    public override string Description => "A round dark-rye loaf, heavy and cracked on top";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new FiberHumor(), new PulpHumor(), new SaltHumor() }
        .GetRange(0, PickHumorCount(rng));
}
