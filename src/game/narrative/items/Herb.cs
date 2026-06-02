using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Herb : ConsumableItem
{
    public override string ItemId      => "herb";
    public override string DisplayName => "Dried Herbs";
    public override string Description => "A bundle of dried culinary herbs, crumbling and faintly fragrant";
    public override ConsumableType ConsumableType => ConsumableType.Inhalant;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<VaporHumor>(55).Add<EtherHumor>(45);
}
