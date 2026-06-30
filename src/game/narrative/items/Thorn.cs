using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Thorn : Item
{
    public override string ItemId => "thorn";
    public override string DisplayName => "Thorn";
    public override string Description => "A hard, curved thorn broken from a branch";
    public override List<ItemTag> Tags => new() { ItemTag.Forage };
    public override int PriceReference => 1;
}
