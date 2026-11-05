using Cathedral.Game.Narrative;
﻿using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class Moss : ConsumableItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Plant });
    public override string ItemId => "moss";
    public override string DisplayName => "Moss";
    public override string Article => "some";
    public override string Description => "A damp clump of dark green moss";
    public override List<ItemTag> Tags => new() { ItemTag.Forage };
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(60).Add<AquaHumor>(40);
}
