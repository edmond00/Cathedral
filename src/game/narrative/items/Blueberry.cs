using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public class Blueberry : Item
{
    public override string ItemId => "blueberry";
    public override string DisplayName => "Blueberry";
    public override string Description => "A small, dark blue berry that grows in forests. Sweet and nutritious.";
    public override List<string> Keywords => new() { "berry", "fruit", "blue" };
}
