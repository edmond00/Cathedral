using Cathedral.Game.Narrative;
using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Lard : ConsumableItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Meat });
    public override string ItemId      => "lard";
    public override string DisplayName => "Lard";
    public override string Article     => "some";
    public override string Description => "A clay crock of rendered pork fat, sealed with a cloth and tied at the neck";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff, ItemTag.Meat };
    public override int PriceReference => 6;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(75).Add<SaltHumor>(15).Add<PhlegmHumor>(10);
}
