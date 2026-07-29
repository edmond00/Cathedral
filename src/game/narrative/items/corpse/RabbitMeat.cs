using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

/// <summary>Raw rabbit butchered from a carcass — lean enough to be poor eating, but eating.</summary>
public sealed class RabbitMeat : ConsumableItem
{
    public override string ItemId      => "rabbit_meat";
    public override string DisplayName => "Rabbit Meat";
    public override string Article     => "some";
    public override string Description => "A lean cut of raw rabbit, the flesh pale and finely grained";
    public override WeightClass Weight       => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override int PriceReference => 6;

    public override ConsumableType ConsumableType => ConsumableType.Food;
    public override bool IsHard => true;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(55).Add<FiberHumor>(35).Add<FatHumor>(10);
}
