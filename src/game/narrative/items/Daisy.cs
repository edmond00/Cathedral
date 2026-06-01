using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Daisy : ConsumableItem
{
    public override string ItemId => "daisy";
    public override string DisplayName => "Daisy";
    public override string Description => "A common white daisy with a yellow centre";
    public override ConsumableType ConsumableType => ConsumableType.Inhalant;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new EtherHumor(), new VaporHumor() }
        .GetRange(0, PickHumorCount(rng));
}
