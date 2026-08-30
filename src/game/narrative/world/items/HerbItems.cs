using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Thyme : HerbItem
{
    public override string ItemId      => "thyme";
    public override string DisplayName => "Thyme";
    public override string Description => "A bundle of woody-stemmed thyme, fragrant and dry";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<VaporHumor>(50).Add<EuphoraHumor>(50);
}

public sealed class Sage : HerbItem
{
    public override string ItemId      => "sage";
    public override string DisplayName => "Sage";
    public override string Description => "A handful of soft grey-green sage leaves";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<VaporHumor>(55).Add<EuphoraHumor>(45);
}

public sealed class Mint : HerbItem
{
    public override string ItemId      => "mint";
    public override string DisplayName => "Mint";
    public override string Description => "A bright sprig of garden mint, leaves cool to the touch";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<VaporHumor>(50).Add<EuphoraHumor>(50);
}

public sealed class Chamomile : HerbItem
{
    public override string ItemId      => "chamomile";
    public override string DisplayName => "Chamomile";
    public override string Description => "A few small white-petalled chamomile flowers, golden-centred";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<EtherHumor>(55).Add<VaporHumor>(45);
}

public sealed class Wormwood : HerbItem
{
    public override string ItemId      => "wormwood";
    public override string DisplayName => "Wormwood";
    public override string Description => "A bitter, silvery-green wormwood stalk";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<OpiumHumor>(55).Add<FumeHumor>(45);
}

public sealed class WildThyme : HerbItem
{
    public override string ItemId      => "wild_thyme";
    public override string DisplayName => "Wild Thyme";
    public override string Description => "A trailing mat of wild thyme, smaller-leaved than the garden kind";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<VaporHumor>(50).Add<EuphoraHumor>(50);
}

public sealed class WildMint : HerbItem
{
    public override string ItemId      => "wild_mint";
    public override string DisplayName => "Wild Mint";
    public override string Description => "A coarse stalk of wild mint, sharper than the cultivated sort";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<VaporHumor>(50).Add<EuphoraHumor>(50);
}

public sealed class Valerian : HerbItem
{
    public override string ItemId      => "valerian";
    public override string DisplayName => "Valerian";
    public override string Description => "A pale-pink valerian umbel on a hollow stem";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<OpiumHumor>(60).Add<EtherHumor>(40);
}

public sealed class Gentian : HerbItem
{
    public override string ItemId      => "gentian";
    public override string DisplayName => "Gentian";
    public override string Description => "A bell-shaped blue gentian flower, rare among high stones";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<OpiumHumor>(40).Add<EtherHumor>(35).Add<YellowBileHumor>(25);
}
