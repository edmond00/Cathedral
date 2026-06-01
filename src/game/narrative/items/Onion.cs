using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Onion : ConsumableItem
{
    public override string ItemId      => "onion";
    public override string DisplayName => "Onion";
    public override string Description => "A dried onion with papery brown skin, pungent and firm";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new FiberHumor(), new VaporHumor(), new FumeHumor() }
        .GetRange(0, PickHumorCount(rng));
}
