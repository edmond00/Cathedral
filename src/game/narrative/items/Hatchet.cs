using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Hatchet : Item
{
    public override string ItemId      => "hatchet";
    public override string DisplayName => "Hatchet";
    public override string Description => "A small single-bit hatchet, the haft smooth from long use";
    public override ItemSize Size      => ItemSize.Medium;
    public override float Weight       => 0.9f;
    public override int   UsageLevel   => 4;
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a small iron <hatchet> leaning against the wall"),
        KeywordInContext.Parse("the smooth worn <haft> of a well-used hatchet"),
    };
}
