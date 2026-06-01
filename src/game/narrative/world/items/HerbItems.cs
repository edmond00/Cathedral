using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Thyme : HerbItem
{
    public override string ItemId      => "thyme";
    public override string DisplayName => "Thyme";
    public override string Description => "A bundle of woody-stemmed thyme, fragrant and dry";
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new VaporHumor(), new EuphoraHumor() }
        .GetRange(0, PickHumorCount(rng));
}

public sealed class Sage : HerbItem
{
    public override string ItemId      => "sage";
    public override string DisplayName => "Sage";
    public override string Description => "A handful of soft grey-green sage leaves";
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new VaporHumor(), new EuphoraHumor() }
        .GetRange(0, PickHumorCount(rng));
}

public sealed class Mint : HerbItem
{
    public override string ItemId      => "mint";
    public override string DisplayName => "Mint";
    public override string Description => "A bright sprig of garden mint, leaves cool to the touch";
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new VaporHumor(), new EuphoraHumor() }
        .GetRange(0, PickHumorCount(rng));
}

public sealed class Chamomile : HerbItem
{
    public override string ItemId      => "chamomile";
    public override string DisplayName => "Chamomile";
    public override string Description => "A few small white-petalled chamomile flowers, golden-centred";
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new VaporHumor(), new EtherHumor() }
        .GetRange(0, PickHumorCount(rng));
}

public sealed class Wormwood : HerbItem
{
    public override string ItemId      => "wormwood";
    public override string DisplayName => "Wormwood";
    public override string Description => "A bitter, silvery-green wormwood stalk";
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new OpiumHumor(), new FumeHumor() }
        .GetRange(0, PickHumorCount(rng));
}

public sealed class WildThyme : HerbItem
{
    public override string ItemId      => "wild_thyme";
    public override string DisplayName => "Wild Thyme";
    public override string Description => "A trailing mat of wild thyme, smaller-leaved than the garden kind";
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new VaporHumor(), new EuphoraHumor() }
        .GetRange(0, PickHumorCount(rng));
}

public sealed class WildMint : HerbItem
{
    public override string ItemId      => "wild_mint";
    public override string DisplayName => "Wild Mint";
    public override string Description => "A coarse stalk of wild mint, sharper than the cultivated sort";
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new VaporHumor(), new EuphoraHumor() }
        .GetRange(0, PickHumorCount(rng));
}

public sealed class Valerian : HerbItem
{
    public override string ItemId      => "valerian";
    public override string DisplayName => "Valerian";
    public override string Description => "A pale-pink valerian umbel on a hollow stem";
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new OpiumHumor(), new FumeHumor() }
        .GetRange(0, PickHumorCount(rng));
}

public sealed class Gentian : HerbItem
{
    public override string ItemId      => "gentian";
    public override string DisplayName => "Gentian";
    public override string Description => "A bell-shaped blue gentian flower, rare among high stones";
    protected override List<BodyHumor> GenerateComposition(Random rng) =>
        new List<BodyHumor> { new OpiumHumor(), new EtherHumor(), new YellowBileHumor() }
        .GetRange(0, PickHumorCount(rng));
}
