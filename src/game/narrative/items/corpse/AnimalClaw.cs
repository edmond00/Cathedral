using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class AnimalClaw : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "animal_claw";
    public override string DisplayName => "Animal Claw";
    public override string Description => "A thick hooked claw, hard as horn and still sharp";
    public override WeightClass Weight       => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Pelt };
    public override int PriceReference => 3;
}
