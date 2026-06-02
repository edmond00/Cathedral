using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class WildBerry : ConsumableItem
{
    public override string ItemId => "wild_berry";
    public override string DisplayName => "Wild Berry";
    public override string Description => "A small dark berry of uncertain edibility";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(50).Add<PulpHumor>(30).Add<FungiHumor>(20);
}
