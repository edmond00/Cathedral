using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Candle : Item
{
    public override string ItemId      => "candle";
    public override string DisplayName => "Tallow Candle";
    public override string Description => "A stubby tallow candle on a clay base, half-burned and wax-spattered";
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a stubby <candle> guttering in the draught"),
        KeywordInContext.Parse("the cold wax <drip> of a long-burned candle"),
    };
}
