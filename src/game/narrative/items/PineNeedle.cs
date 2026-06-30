using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class PineNeedle : ConsumableItem
{
    public override string ItemId => "pine_needle";
    public override string DisplayName => "Pine Needles";
    public override string Description => "A small cluster of stiff, sharp pine needles";
    public override List<ItemTag> Tags => new() { ItemTag.Forage };
    public override int PriceReference => 1;
    public override ConsumableType ConsumableType => ConsumableType.Inhalant;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<VaporHumor>(45).Add<EuphoraHumor>(30).Add<EtherHumor>(25);
}
