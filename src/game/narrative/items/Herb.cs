using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Herb : Item
{
    public override string ItemId      => "herb";
    public override string DisplayName => "Dried Herbs";
    public override string Description => "A bundle of dried culinary herbs, crumbling and faintly fragrant";
}
