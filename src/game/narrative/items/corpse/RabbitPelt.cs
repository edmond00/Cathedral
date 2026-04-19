using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class RabbitPelt : Item
{
    public override string ItemId      => "rabbit_pelt";
    public override string DisplayName => "Rabbit Pelt";
    public override string Description => "A soft grey pelt, thin-skinned and still attached to a layer of fat";
    public override float Weight       => 0.15f;
}
