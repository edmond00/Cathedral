using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Items;

public sealed class DriedMeat : ConsumableItem
{
    public override string ItemId      => "dried_meat";
    public override string DisplayName => "Dried Meat";
    public override string Description => "A strip of salted dark meat, hard and leathery, smelling of brine";
    public override ConsumableType ConsumableType => ConsumableType.Food;
    public override bool IsHard => true;
    protected override HumorRichness Richness => HumorRichness.Rich;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(40).Add<SaltHumor>(35).Add<FatHumor>(25);
}
