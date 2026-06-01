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
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new FiberHumor(), new SaltHumor(), new PulpHumor() }
        .GetRange(0, PickHumorCount(rng));
}
