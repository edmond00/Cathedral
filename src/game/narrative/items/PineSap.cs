using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class PineSap : ConsumableItem
{
    public override string ItemId => "pine_sap";
    public override string DisplayName => "Pine Sap";
    public override string Description => "A sticky bead of amber-coloured pine resin";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new SaltHumor(), new CalxHumor(), new PhlegmHumor() }
        .GetRange(0, PickHumorCount(rng));
}
