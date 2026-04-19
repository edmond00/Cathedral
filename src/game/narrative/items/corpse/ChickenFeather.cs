using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class ChickenFeather : Item
{
    public override string ItemId      => "chicken_feather";
    public override string DisplayName => "Chicken Feather";
    public override string Description => "A cream-coloured flight feather, slightly curved and still clean";
    public override float Weight       => 0.001f;
}
