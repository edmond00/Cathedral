using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Milk : ConsumableItem
{
    public override string ItemId      => "milk";
    public override string DisplayName => "Milk";
    public override string Description => "A wooden pail of fresh, faintly warm milk";
    public override float Weight => 1.0f;
    public override ConsumableType ConsumableType => ConsumableType.Drink;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new SerumHumor(), new AquaHumor(), new SaltHumor() }
        .GetRange(0, PickHumorCount(rng));
}

public sealed class Butter : Item
{
    public override string ItemId      => "butter";
    public override string DisplayName => "Butter";
    public override string Description => "A pale block of fresh butter wrapped in a leaf";
    public override float Weight => 0.4f;
}
