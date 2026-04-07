using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class PineSap : Item
{
    public override string ItemId => "pine_sap";
    public override string DisplayName => "Pine Sap";
    public override string Description => "A sticky bead of amber-coloured pine resin";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a sticky <resin> seeping from the bark"),
        KeywordInContext.Parse("a bead of amber <sap> caught on the trunk"),
    };
}
