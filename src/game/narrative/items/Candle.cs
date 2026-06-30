using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Candle : Item
{
    public override string ItemId      => "candle";
    public override string DisplayName => "Tallow Candle";
    public override string Description => "A stubby tallow candle on a clay base, half-burned and wax-spattered";
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 4;
}
