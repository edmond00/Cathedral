using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Sickle : Item
{
    public override string ItemId      => "sickle";
    public override string DisplayName => "Sickle";
    public override string Description => "A short iron sickle with a curved blade, edge nicked from years of harvest";
    public override ItemSize Size      => ItemSize.Medium;
    public override float Weight       => 0.5f;
    public override int   UsageLevel   => 4;
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("the curved iron <sickle> hanging from the wall"),
        KeywordInContext.Parse("the nicked <blade> of a well-used harvest sickle"),
    };
}
