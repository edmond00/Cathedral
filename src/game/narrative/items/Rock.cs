using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Rock : Item
{
    public override string ItemId => "rock";
    public override string DisplayName => "Rock";
    public override string Description => "A fist-sized rock broken from a boulder face";
    public override List<ItemTag> Tags => new() { ItemTag.Mineral };
    public override int PriceReference => 2;
}
