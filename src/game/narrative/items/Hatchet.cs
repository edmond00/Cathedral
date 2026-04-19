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
}
