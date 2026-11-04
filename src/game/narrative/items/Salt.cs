using Cathedral.Game.Narrative;
﻿using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Salt : ConsumableItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Food });
    public override string ItemId      => "salt";
    public override Rarity Rarity => Rarity.Scarce;
    public override string DisplayName => "Salt";
    public override string Article     => "some";
    public override string Description => "A small cloth parcel of coarse grey salt, damp at the edges";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe().Add<SaltHumor>(100);
}
