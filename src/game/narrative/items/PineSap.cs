using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class PineSap : ConsumableItem
{
    public override string ItemId => "pine_sap";
    public override string DisplayName => "Pine Sap";
    public override string Article => "some";
    public override string Description => "A sticky bead of amber-coloured pine resin";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(45).Add<PhlegmHumor>(35).Add<CalxHumor>(20);
}
