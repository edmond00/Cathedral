using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class LeatherBelt : Item
{
    public override string ItemId           => "leather_belt";
    public override string DisplayName      => "Leather Belt";
    public override string Description      => "A thick leather belt with a plain iron buckle, creased and darkened";
    public override List<ItemType> Types    => new() { ItemType.BeltGear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.BeltGear;
}
