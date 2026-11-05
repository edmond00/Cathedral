using Cathedral.Game.Narrative;
﻿using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class BushLeaf : ConsumableItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Plant });
    public override string ItemId => "bush_leaf";
    public override string DisplayName => "Bush Leaf";
    public override string Description => "A small, slightly leathery leaf from a thorny bush";
    public override List<ItemTag> Tags => new() { ItemTag.Forage };
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FiberHumor>(80).Add<YellowBileHumor>(20);
}
