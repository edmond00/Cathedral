using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public class Trout : Item
{
    public override string ItemId => "trout";
    public override string DisplayName => "Trout";
    public override string Description => "A freshwater fish with spotted scales. Can be eaten raw or cooked.";
    public override List<string> Keywords => new() { "fish", "water", "scales" };
}
