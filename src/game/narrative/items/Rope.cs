using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Rope : Item
{
    public override ItemCategory Category => ItemCategory.Tool;
    public override string ItemId      => "rope";
    public override string DisplayName => "Rope";
    public override string Description => "A coil of twisted hemp rope, thick and rough-fibred";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass Weight       => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 6;
    public override int UsageLevel     => 4;

    // Deliberately declares no MadeForVerbIds. A rope obviously serves the climbs, and naming them
    // would spare an LLM round trip on each — but that declaration is exclusive, and a rope is the
    // opposite of single-purpose: it binds, hauls, lowers, snares and ties, across acts nobody has
    // enumerated. Naming four would forbid the rest. General implements are judged on their merits.
}
