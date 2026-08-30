using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Pear : FruitItem
{
    public override string ItemId      => "pear";
    public override string DisplayName => "Pear";
    public override string Description => "A speckled green pear, narrow at the stem and heavy at the base";
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PulpHumor>(55).Add<SugarHumor>(30).Add<FiberHumor>(15);
}

public sealed class Plum : FruitItem
{
    public override string ItemId      => "plum";
    public override string DisplayName => "Plum";
    public override string Description => "A small purple plum with a dusty bloom on its skin";
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PulpHumor>(50).Add<SugarHumor>(35).Add<FiberHumor>(15);
}

public sealed class Cherry : FruitItem
{
    public override string ItemId      => "cherry";
    public override string DisplayName => "Cherry";
    public override string Description => "A small dark cherry, glossy and tart";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(45).Add<PulpHumor>(40).Add<FiberHumor>(15);
}

public sealed class Blackberry : FruitItem
{
    public override string ItemId      => "blackberry";
    public override string DisplayName => "Blackberry";
    public override string Description => "A cluster of glossy black drupelets, juice already staining your fingers";
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(45).Add<PulpHumor>(35).Add<FiberHumor>(20);
}

public sealed class Bilberry : FruitItem
{
    public override string ItemId      => "bilberry";
    public override string DisplayName => "Bilberry";
    public override string Description => "A handful of small dusky-blue bilberries, tart and dark";
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(50).Add<PulpHumor>(30).Add<FiberHumor>(20);
}

public sealed class Sloe : FruitItem
{
    public override string ItemId      => "sloe";
    public override string DisplayName => "Sloe";
    public override string Description => "A blue-black sloe, hard and astringent until softened by frost";
    public override bool IsHard => true;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FumeHumor>(40).Add<FiberHumor>(35).Add<SugarHumor>(25);
}

public sealed class HawthornBerry : FruitItem
{
    public override string ItemId      => "hawthorn_berry";
    public override string DisplayName => "Hawthorn Berry";
    public override string Description => "A small red haw, tough-skinned and faintly sweet";
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(50).Add<SugarHumor>(30).Add<CalxHumor>(20);
}

public sealed class Elderberry : FruitItem
{
    public override string ItemId      => "elderberry";
    public override string DisplayName => "Elderberry";
    public override string Description => "A dense umbel of tiny black elderberries on a fragile stem";
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(40).Add<PulpHumor>(35).Add<FungiHumor>(25);
}

public sealed class Acorn : FruitItem
{
    public override string ItemId      => "acorn";
    public override string DisplayName => "Acorn";
    public override string Description => "A smooth oak acorn cupped in a rough scaly hat";
    public override bool IsHard => true;
    public override List<ItemTag> Tags => new() { ItemTag.Crop, ItemTag.Forage };
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(35).Add<FiberHumor>(30).Add<YellowBileHumor>(20).Add<CalxHumor>(15);
}

public sealed class Beechnut : FruitItem
{
    public override string ItemId      => "beechnut";
    public override string DisplayName => "Beechnut";
    public override string Description => "A small triangular beech-mast, oily and slightly bitter";
    public override bool IsHard => true;
    public override List<ItemTag> Tags => new() { ItemTag.Crop, ItemTag.Forage };
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(45).Add<FiberHumor>(30).Add<YellowBileHumor>(25);
}
