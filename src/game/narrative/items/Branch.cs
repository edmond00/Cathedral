using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

/// <summary>A woody branch broken from a tree. Shared by apple tree and pine tree.</summary>
public sealed class Branch : Item
{
    public override string ItemId => "branch";
    public override string DisplayName => "Branch";
    public override string Description => "A sturdy branch snapped from a tree";
}
