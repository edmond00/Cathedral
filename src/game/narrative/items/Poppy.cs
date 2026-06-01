using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Poppy : ConsumableItem
{
    public override string ItemId => "poppy";
    public override string DisplayName => "Poppy";
    public override string Description => "A vivid red poppy, its petals paper-thin";
    public override ConsumableType ConsumableType => ConsumableType.Inhalant;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new OpiumHumor(), new EuphoraHumor(), new FumeHumor() }
        .GetRange(0, PickHumorCount(rng));
}
