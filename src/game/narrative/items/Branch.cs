using Cathedral.Game.Narrative;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

/// <summary>
/// A woody branch broken from a tree. Shared by apple tree and pine tree.
///
/// <para>An improvised club: it opens the blunt fighting skills, but at <see cref="Level"/> 0 lends
/// none of a weapon's dice — that is what a <see cref="Cudgel"/>, trimmed for it, is for. As a tool it
/// is a length of wood, good for reaching and poking and little else, so it lends one die where the
/// blunt category's shape would lend four.</para>
/// </summary>
public sealed class Branch : WeaponItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Bludgeon, ItemKind.Wood });
    public override string ItemId         => "branch";
    public override string DisplayName    => "Branch";
    public override string Description    => "A sturdy branch snapped from a tree";
    public override int    Level          => 0;
    public override string WeaponCategory => "blunt";
    public override int    UsageLevel     => 1;
    // Timber, not the blacksmith's Ironwork, and priced as the wood it is.
    public override List<ItemTag> Tags    => new() { ItemTag.Wood, ItemTag.Timber };
    public override int PriceReference    => 3;
}
