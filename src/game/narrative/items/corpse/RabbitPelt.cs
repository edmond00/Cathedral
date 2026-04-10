using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class RabbitPelt : Item
{
    public override string ItemId      => "rabbit_pelt";
    public override string DisplayName => "Rabbit Pelt";
    public override string Description => "A soft grey pelt, thin-skinned and still attached to a layer of fat";
    public override float Weight       => 0.15f;
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a soft grey <pelt> peeled from the carcass"),
        KeywordInContext.Parse("the fine dense <fur> of a rabbit skin"),
    };
}
