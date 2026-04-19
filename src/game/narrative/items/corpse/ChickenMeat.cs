using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class ChickenMeat : Item
{
    public override string ItemId      => "chicken_meat";
    public override string DisplayName => "Chicken Meat";
    public override string Description => "A raw cut of pale poultry, still warm from the carcass";
    public override float Weight       => 0.3f;
}
