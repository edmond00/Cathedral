using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class LinenTunic : Item
{
    public override string ItemId           => "linen_tunic";
    public override string DisplayName      => "Linen Tunic";
    public override string Description      => "A coarse off-white linen tunic, rough at the collar and well-mended";
    public override ItemSize Size           => ItemSize.Medium;
    public override List<ItemType> Types    => new() { ItemType.Bodywear };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.Bodywear;
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a coarse off-white linen <tunic> folded on the shelf"),
        KeywordInContext.Parse("the rough <collar> of a well-mended working tunic"),
    };
}
