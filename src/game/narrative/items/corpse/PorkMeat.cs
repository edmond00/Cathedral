using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

/// <summary>
/// Raw pork butchered from a carcass. A <see cref="ConsumableItem"/> rather than a plain item:
/// as an <c>Item</c> it could be carried and sold but never eaten, which is not what meat is for.
/// Raw and heavy going, so it needs teeth.
/// </summary>
public sealed class PorkMeat : ConsumableItem
{
    public override string ItemId      => "pork_meat";
    public override string DisplayName => "Pork Meat";
    public override string Article     => "some";
    public override string Description => "A heavy cut of raw pork, marbled with fat and still bleeding";
    public override WeightClass Weight       => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override int PriceReference => 8;

    public override ConsumableType ConsumableType => ConsumableType.Food;
    public override bool IsHard => true;
    protected override HumorRichness Richness => HumorRichness.Rich;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(45).Add<FatHumor>(40).Add<FiberHumor>(15);
}
