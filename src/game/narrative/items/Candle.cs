using Cathedral.Game.Narrative;
﻿using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Candle : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Light });
    public override ItemCategory Category => ItemCategory.Tool;
    public override string ItemId      => "candle";
    public override string DisplayName => "Tallow Candle";
    public override string Description => "A stubby tallow candle on a clay base, half-burned and wax-spattered";
    public override List<ItemTag> Tags => new() { ItemTag.Craftware, ItemTag.Chandlery };
    public override int PriceReference => 4;
}
