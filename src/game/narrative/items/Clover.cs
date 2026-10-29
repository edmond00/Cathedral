using Cathedral.Game.Narrative;
﻿using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Clover : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Plant, ItemKind.Fodder, ItemKind.Flower });
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId => "clover";
    public override string DisplayName => "Clover";
    public override string Article => "some";
    public override string Description => "A sprig of three-leafed clover with a small pink head";
    public override List<ItemTag> Tags => new() { ItemTag.Herb };
    public override int PriceReference => 1;
}
