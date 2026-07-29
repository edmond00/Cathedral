using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class RabbitPelt : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "rabbit_pelt";
    public override string DisplayName => "Rabbit Pelt";
    public override string Description => "A soft grey pelt, thin-skinned and still attached to a layer of fat";
    public override WeightClass Weight       => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Pelt };
    public override int PriceReference => 6;
}
