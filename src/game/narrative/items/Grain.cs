using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Grain : Item
{
    public override string ItemId      => "grain";
    public override string DisplayName => "Grain";
    public override string Description => "A small cloth sack of dried wheat grain, heavy and husked";
    public override ItemSize Size      => ItemSize.Medium;
}
