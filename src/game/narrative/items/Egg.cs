using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Egg : ConsumableItem
{
    public override string ItemId      => "egg";
    public override string DisplayName => "Egg";
    public override string Description => "A brown hen's egg, warm and faintly spotted";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new FatHumor(), new SerumHumor(), new FiberHumor() }
        .GetRange(0, PickHumorCount(rng));
}
