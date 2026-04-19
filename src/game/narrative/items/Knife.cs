using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Knife : Item
{
    public override string ItemId      => "knife";
    public override string DisplayName => "Knife";
    public override string Description => "A short-bladed knife with a worn wooden handle, kept sharp";
    public override int    UsageLevel  => 3;
}
