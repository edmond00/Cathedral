using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Dandelion : ConsumableItem
{
    public override string ItemId => "dandelion";
    public override string DisplayName => "Dandelion";
    public override string Description => "A dandelion in seed, its white globe ready to scatter";
    public override ConsumableType ConsumableType => ConsumableType.Inhalant;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new VaporHumor(), new EuphoraHumor() }
        .GetRange(0, PickHumorCount(rng));
}
