using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Turnip : ConsumableItem
{
    public override string ItemId      => "turnip";
    public override string DisplayName => "Turnip";
    public override string Description => "A knobbed purple-white root, still clodded with earth";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new FiberHumor(), new PulpHumor(), new SaltHumor() }
        .GetRange(0, PickHumorCount(rng));
}
