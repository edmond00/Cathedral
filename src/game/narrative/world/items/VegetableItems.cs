using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Radish : VegetableItem
{
    public override string ItemId      => "radish";
    public override string DisplayName => "Radish";
    public override string Description => "A small red-skinned radish, peppery and crisp";
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new FiberHumor(), new VaporHumor(), new FumeHumor() }
        .GetRange(0, PickHumorCount(rng));
}

public sealed class Parsnip : VegetableItem
{
    public override string ItemId      => "parsnip";
    public override string DisplayName => "Parsnip";
    public override string Description => "A pale tapering parsnip, sweet when cooked";
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new FiberHumor(), new SugarHumor(), new CalxHumor() }
        .GetRange(0, PickHumorCount(rng));
}

public sealed class Leek : VegetableItem
{
    public override string ItemId      => "leek";
    public override string DisplayName => "Leek";
    public override string Description => "A long-stalked leek with a white root and dark-green leaves";
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new FiberHumor(), new VaporHumor() }
        .GetRange(0, PickHumorCount(rng));
}

public sealed class Pea : VegetableItem
{
    public override string ItemId      => "pea";
    public override string DisplayName => "Peas";
    public override string Description => "A handful of fresh peas in their pods";
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new FiberHumor(), new PulpHumor(), new SaltHumor() }
        .GetRange(0, PickHumorCount(rng));
}

public sealed class Beetroot : VegetableItem
{
    public override string ItemId      => "beetroot";
    public override string DisplayName => "Beetroot";
    public override string Description => "A deep-purple beetroot, earth-stained and heavy";
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new FiberHumor(), new PulpHumor(), new BloodHumor() }
        .GetRange(0, PickHumorCount(rng));
}
