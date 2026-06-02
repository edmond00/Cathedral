using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Grain : ConsumableItem
{
    public override string ItemId      => "grain";
    public override string DisplayName => "Grain";
    public override string Description => "A small cloth sack of dried wheat grain, heavy and husked";
    public override ItemSize Size      => ItemSize.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    public override bool IsHard => true;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(55).Add<CalxHumor>(25).Add<PulpHumor>(20);
}
