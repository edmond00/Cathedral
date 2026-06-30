using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Egg : ConsumableItem
{
    public override string ItemId      => "egg";
    public override string DisplayName => "Egg";
    public override string Description => "A brown hen's egg, warm and faintly spotted";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override int PriceReference => 1;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(40).Add<SerumHumor>(40).Add<CalxHumor>(20);
}
