using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Cheese : ConsumableItem
{
    public override string ItemId      => "cheese";
    public override string DisplayName => "Cheese";
    public override string Description => "A wedge of aged yellow cheese, firm-rinded and sharp-smelling";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new SerumHumor(), new FatHumor(), new SaltHumor() }
        .GetRange(0, PickHumorCount(rng));
}
