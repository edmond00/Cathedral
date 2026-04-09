using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class LeatherBoots : Item
{
    public override string ItemId           => "leather_boots";
    public override string DisplayName      => "Leather Boots";
    public override string Description      => "A pair of heavy ankle boots, calf leather worn pale at the toes";
    public override ItemSize Size           => ItemSize.Medium;
    public override List<ItemType> Types    => new() { ItemType.Footwear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Footwear;
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a pair of heavy leather <boots> by the door"),
        KeywordInContext.Parse("the worn pale <toe> of a well-used leather boot"),
    };
}
