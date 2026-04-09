using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Rope : Item
{
    public override string ItemId      => "rope";
    public override string DisplayName => "Rope";
    public override string Description => "A coil of twisted hemp rope, thick and rough-fibred";
    public override ItemSize Size      => ItemSize.Medium;
    public override float Weight       => 0.8f;
    public override List<KeywordInContext> OutcomeKeywordsInContext => new()
    {
        KeywordInContext.Parse("a coil of rough hemp <rope> hanging from a peg"),
        KeywordInContext.Parse("the frayed <end> of a twisted hemp coil"),
    };
}
