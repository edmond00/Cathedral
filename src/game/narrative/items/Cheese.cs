using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Cheese : ConsumableItem
{
    public override string ItemId      => "cheese";
    public override string DisplayName => "Cheese";
    public override string Description => "A wedge of aged yellow cheese, firm-rinded and sharp-smelling";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Rich;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(45).Add<SaltHumor>(25).Add<SerumHumor>(20).Add<FungiHumor>(10);
}
