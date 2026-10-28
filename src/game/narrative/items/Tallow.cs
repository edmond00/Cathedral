using Cathedral.Game.Narrative;
﻿using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Tallow : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Fuel, ItemKind.Light });
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "tallow";
    public override string DisplayName => "Tallow";
    public override string Article     => "some";
    public override string Description => "A lump of rendered animal fat, pale and waxy, with a faint rancid smell";
    public override int PriceReference => 4;
}
