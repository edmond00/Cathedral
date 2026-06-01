using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Clay : ConsumableItem
{
    public override string ItemId      => "clay";
    public override string DisplayName => "Clay";
    public override string Description => "A wet lump of grey-brown clay, cool and dense in the hand";
    public override float Weight => 0.6f;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new SaltHumor(), new PhlegmHumor(), new BlackBileHumor() }
        .GetRange(0, PickHumorCount(rng));
}

public sealed class Lichen : ConsumableItem
{
    public override string ItemId      => "lichen";
    public override string DisplayName => "Lichen";
    public override string Description => "A papery crust of grey-green lichen prised from rock";
    public override float Weight => 0.05f;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new FiberHumor(), new YellowBileHumor(), new PhlegmHumor() }
        .GetRange(0, PickHumorCount(rng));
}
