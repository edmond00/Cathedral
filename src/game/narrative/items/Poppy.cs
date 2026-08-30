using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Poppy : ConsumableItem
{
    public override string ItemId => "poppy";
    public override string DisplayName => "Poppy";
    public override string Description => "A vivid red poppy, its petals paper-thin";
    public override List<ItemTag> Tags => new() { ItemTag.Herb };
    public override int PriceReference => 3;
    public override ConsumableType ConsumableType => ConsumableType.Inhalant;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<OpiumHumor>(60).Add<EuphoraHumor>(25).Add<FumeHumor>(15);
}
