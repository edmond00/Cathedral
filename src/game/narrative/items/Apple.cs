using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Apple : Item
{
    public override string ItemId => "apple";
    public override string DisplayName => "Apple";
    public override string Description => "A ripe apple, red-green and faintly bruised";
    public override List<ItemType> Types => new() { ItemType.Other };
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a ripe <apple> fallen in the grass"),
        KeywordInContext.Parse("the sweet <flesh> of a windfall apple"),
    };
}
