using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Grain : ConsumableItem
{
    public override string ItemId      => "grain";
    public override string DisplayName => "Grain";
    public override string Description => "A small cloth sack of dried wheat grain, heavy and husked";
    public override ItemSize Size      => ItemSize.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    public override bool IsHard => true;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new FiberHumor(), new PulpHumor(), new SaltHumor() }
        .GetRange(0, PickHumorCount(rng));
}
