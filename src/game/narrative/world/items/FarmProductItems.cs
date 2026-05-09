using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

public sealed class Milk : AnimalProductItem
{
    public override string ItemId      => "milk";
    public override string DisplayName => "Milk";
    public override string Description => "A wooden pail of fresh, faintly warm milk";
    public override float Weight => 1.0f;
    public override List<ItemType> Types => new() { ItemType.Liquid };
}

public sealed class Butter : AnimalProductItem
{
    public override string ItemId      => "butter";
    public override string DisplayName => "Butter";
    public override string Description => "A pale block of fresh butter wrapped in a leaf";
    public override float Weight => 0.4f;
}
