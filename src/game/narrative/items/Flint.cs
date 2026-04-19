using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Flint : Item
{
    public override string ItemId      => "flint";
    public override string DisplayName => "Flint";
    public override string Description => "A sharp-edged flint nodule, one face knapped flat for striking fire";
    public override int    UsageLevel  => 2;
}
