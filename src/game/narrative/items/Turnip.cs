using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Turnip : Item
{
    public override string ItemId      => "turnip";
    public override string DisplayName => "Turnip";
    public override string Description => "A knobbed purple-white root, still clodded with earth";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a stout <turnip> half-buried in the garden soil"),
        KeywordInContext.Parse("the pale <root> of a turnip jutting from the earth"),
    };
}
