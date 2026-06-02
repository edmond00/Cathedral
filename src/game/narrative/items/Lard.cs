using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Lard : ConsumableItem
{
    public override string ItemId      => "lard";
    public override string DisplayName => "Lard";
    public override string Description => "A clay crock of rendered pork fat, sealed with a cloth and tied at the neck";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(75).Add<SaltHumor>(15).Add<PhlegmHumor>(10);
}
