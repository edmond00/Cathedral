using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Tallow : Item
{
    public override string ItemId      => "tallow";
    public override string DisplayName => "Tallow";
    public override string Description => "A lump of rendered animal fat, pale and waxy, with a faint rancid smell";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a pale lump of <tallow> wrapped in cloth"),
        KeywordInContext.Parse("the greasy <fat> smell of rendered animal tallow"),
    };
}
