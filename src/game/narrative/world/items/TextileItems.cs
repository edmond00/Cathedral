using Cathedral.Game.Narrative;
﻿namespace Cathedral.Game.Narrative.World.Items;

public sealed class Thread : TextileItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Fibre });
    public override string ItemId      => "thread";
    public override List<ItemTag> Tags => new() { ItemTag.Textile, ItemTag.Cloth };
    public override string DisplayName => "Thread";
    public override string Description => "A small spool of spun thread, ends fraying";
}

public sealed class Cloth : TextileItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Cloth });
    public override string ItemId      => "cloth";
    public override List<ItemTag> Tags => new() { ItemTag.Textile, ItemTag.Cloth };
    public override string DisplayName => "Cloth";
    public override string Description => "A folded length of plain woven cloth, undyed";
    public override WeightClass Weight => WeightClass.Light;
}

public sealed class Flax : TextileItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Fibre, ItemKind.Plant });
    public override string ItemId      => "flax";
    public override string DisplayName => "Flax";
    public override string Description => "A bundle of pale dried flax stems ready for retting";
}

public sealed class Linen : TextileItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Cloth });
    public override string ItemId      => "linen";
    public override List<ItemTag> Tags => new() { ItemTag.Textile, ItemTag.Cloth };
    public override string DisplayName => "Linen";
    public override string Description => "A folded sheet of fine linen, smooth and cool to the touch";
    public override WeightClass Weight => WeightClass.Light;
}
