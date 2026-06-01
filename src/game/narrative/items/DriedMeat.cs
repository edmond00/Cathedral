using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class DriedMeat : ConsumableItem
{
    public override string ItemId      => "dried_meat";
    public override string DisplayName => "Dried Meat";
    public override string Description => "A strip of salted dark meat, hard and leathery, smelling of brine";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    public override bool IsHard => true;
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new BloodHumor(), new SaltHumor(), new FatHumor() }
        .GetRange(0, PickHumorCount(rng));
}
