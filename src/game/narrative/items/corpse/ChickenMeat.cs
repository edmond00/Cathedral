using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

/// <summary>Raw poultry butchered from a carcass — edible, if you are prepared to eat it raw.</summary>
public sealed class ChickenMeat : ConsumableItem
{
    public override string ItemId      => "chicken_meat";
    public override string DisplayName => "Chicken Meat";
    public override string Article     => "some";
    public override string Description => "A raw cut of pale poultry, still warm from the carcass";
    public override WeightClass Weight       => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override int PriceReference => 6;

    public override ConsumableType ConsumableType => ConsumableType.Food;
    public override bool IsHard => true;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(50).Add<FiberHumor>(30).Add<FatHumor>(20);
}
