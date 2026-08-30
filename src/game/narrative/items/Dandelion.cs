using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Dandelion : ConsumableItem
{
    public override string ItemId => "dandelion";
    public override string DisplayName => "Dandelion";
    public override string Description => "A dandelion in seed, its white globe ready to scatter";
    public override List<ItemTag> Tags => new() { ItemTag.Herb };
    public override int PriceReference => 1;
    public override ConsumableType ConsumableType => ConsumableType.Inhalant;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<VaporHumor>(55).Add<EuphoraHumor>(45);
}
