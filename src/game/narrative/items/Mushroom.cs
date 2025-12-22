using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public class Mushroom : Item
{
    public override string ItemId => "mushroom";
    public override string DisplayName => "Mushroom";
    public override string Description => "A wild mushroom with a pale cap. Potentially edible or medicinal.";
    public override List<string> Keywords => new() { "mushroom", "fungus", "cap" };
}
