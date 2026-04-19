using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Torch : Item
{
    public override string ItemId      => "torch";
    public override string DisplayName => "Torch";
    public override string Description => "A pine-resin torch on a short wooden handle, the head wrapped in charred cloth";
    public override ItemSize Size      => ItemSize.Medium;
    public override int   UsageLevel   => 2;
}
