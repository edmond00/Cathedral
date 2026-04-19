using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Hay : Item
{
    public override string ItemId      => "hay";
    public override string DisplayName => "Hay";
    public override string Description => "A tied bundle of dried grass, rough and dust-smelling";
    public override ItemSize Size      => ItemSize.Medium;
}
