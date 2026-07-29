using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class AnimalFang : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "animal_fang";
    public override string DisplayName => "Animal Fang";
    public override string Description => "A curved ivory fang, still slick with blood at the root";
    public override WeightClass Weight       => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Pelt };
    public override int PriceReference => 3;
}
