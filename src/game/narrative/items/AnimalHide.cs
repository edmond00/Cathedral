using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class AnimalHide : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "animal_hide";
    public override string DisplayName => "Animal Hide";
    public override string Description => "A scraped and dried animal hide, stiff and yellowed, smelling of salt and work";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass Weight       => WeightClass.Medium;
    public override int PriceReference => 15;
}
