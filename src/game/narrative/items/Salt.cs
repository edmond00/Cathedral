using Cathedral.Game.Narrative;
﻿using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Salt : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Food });
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "salt";
    public override string DisplayName => "Salt";
    public override string Article     => "some";
    public override string Description => "A small cloth parcel of coarse grey salt, damp at the edges";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override int PriceReference => 10;
}
