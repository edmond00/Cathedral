using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class PineCone : Item
{
    public override string ItemId => "pine_cone";
    public override string DisplayName => "Pine Cone";
    public override string Description => "A dry, open pine cone dropped from the tree";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a dry <cone> lying open in the needles"),
        KeywordInContext.Parse("the woody <scale>s of a fallen pine cone"),
    };
}
