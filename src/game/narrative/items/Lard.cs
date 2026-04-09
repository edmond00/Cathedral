using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Lard : Item
{
    public override string ItemId      => "lard";
    public override string DisplayName => "Lard";
    public override string Description => "A clay crock of rendered pork fat, sealed with a cloth and tied at the neck";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a clay <crock> of pale rendered lard on the shelf"),
        KeywordInContext.Parse("the cloth <seal> tied around the neck of the lard crock"),
    };
}
