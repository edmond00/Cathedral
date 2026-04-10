using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class AnimalFang : Item
{
    public override string ItemId      => "animal_fang";
    public override string DisplayName => "Animal Fang";
    public override string Description => "A curved ivory fang, still slick with blood at the root";
    public override float Weight       => 0.05f;
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a curved yellow <fang> pulled from the jaw"),
        KeywordInContext.Parse("the sharp ivory <tooth> of a predator"),
    };
}
