using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class WildBerry : ConsumableItem
{
    public override string ItemId => "wild_berry";
    public override string DisplayName => "Wild Berry";
    public override string Description => "A small dark berry of uncertain edibility";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new SugarHumor(), new FumeHumor(), new FungiHumor() }
        .GetRange(0, PickHumorCount(rng));
}
