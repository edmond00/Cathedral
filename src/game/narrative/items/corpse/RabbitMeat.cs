using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class RabbitMeat : Item
{
    public override string ItemId      => "rabbit_meat";
    public override string DisplayName => "Rabbit Meat";
    public override string Description => "A lean cut of raw rabbit, the flesh pale and finely grained";
    public override float Weight       => 0.2f;
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a lean cut of raw <rabbit> from the hindquarters"),
        KeywordInContext.Parse("the pale fine <flesh> of a small game animal"),
    };
}
