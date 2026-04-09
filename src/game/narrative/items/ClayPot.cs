using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class ClayPot : Item
{
    public override string ItemId      => "clay_pot";
    public override string DisplayName => "Clay Pot";
    public override string Description => "A squat clay cooking pot, fire-blackened and sealed with a plug of wax";
    public override ItemSize Size      => ItemSize.Medium;
    public override float Weight       => 0.6f;
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a squat fire-blackened <pot> of coarse clay"),
        KeywordInContext.Parse("the wax <plug> sealing the mouth of the clay pot"),
    };
}
