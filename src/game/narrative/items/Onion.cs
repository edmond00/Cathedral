using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Onion : ConsumableItem
{
    public override string ItemId      => "onion";
    public override string DisplayName => "Onion";
    public override string Description => "A dried onion with papery brown skin, pungent and firm";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(45).Add<VaporHumor>(30).Add<PulpHumor>(25);
}
