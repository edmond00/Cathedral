using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Radish : VegetableItem
{
    public override string ItemId      => "radish";
    public override string DisplayName => "Radish";
    public override string Description => "A small red-skinned radish, peppery and crisp";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(45).Add<VaporHumor>(30).Add<PulpHumor>(25);
}

public sealed class Parsnip : VegetableItem
{
    public override string ItemId      => "parsnip";
    public override string DisplayName => "Parsnip";
    public override string Description => "A pale tapering parsnip, sweet when cooked";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(40).Add<SugarHumor>(30).Add<PulpHumor>(30);
}

public sealed class Leek : VegetableItem
{
    public override string ItemId      => "leek";
    public override string DisplayName => "Leek";
    public override string Description => "A long-stalked leek with a white root and dark-green leaves";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(45).Add<VaporHumor>(35).Add<PulpHumor>(20);
}

public sealed class Pea : VegetableItem
{
    public override string ItemId      => "pea";
    public override string DisplayName => "Peas";
    public override string Description => "A handful of fresh peas in their pods";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(40).Add<PulpHumor>(35).Add<SugarHumor>(25);
}

public sealed class Beetroot : VegetableItem
{
    public override string ItemId      => "beetroot";
    public override string DisplayName => "Beetroot";
    public override string Description => "A deep-purple beetroot, earth-stained and heavy";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PulpHumor>(40).Add<FiberHumor>(30).Add<SugarHumor>(20).Add<BloodHumor>(10);
}
