using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Torch : Item
{
    public override string ItemId      => "torch";
    public override string DisplayName => "Torch";
    public override string Description => "A pine-resin torch on a short wooden handle, the head wrapped in charred cloth";
    public override ItemSize Size      => ItemSize.Medium;
    public override int   UsageLevel   => 2;
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("an unlit <torch> leaning against the wall"),
        KeywordInContext.Parse("the charred <cloth> head of an unlit resin torch"),
    };
}
