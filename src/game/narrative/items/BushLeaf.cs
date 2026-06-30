using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class BushLeaf : Item
{
    public override string ItemId => "bush_leaf";
    public override string DisplayName => "Bush Leaf";
    public override string Description => "A small, slightly leathery leaf from a thorny bush";
    public override List<ItemTag> Tags => new() { ItemTag.Forage };
    public override int PriceReference => 1;
}
