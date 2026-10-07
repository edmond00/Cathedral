using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

// ─────────────────────────────────────────────────────────────────────────────
//  What the great buildings keep: the written things of a library or a chancery, the washing and
//  dressing of a bathhouse and an infirmary, a chapel's incense and a cellar's wine. Ordinary
//  catalogue items — they go on shelves, into packs and across counters like anything else.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class Tome : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "tome";
    public override string DisplayName => "Tome";
    public override string Description => "A heavy book bound in boards and calf, its pages close-written in a brown hand";
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override int PriceReference => 40;
}

public sealed class Parchment : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "parchment";
    public override string DisplayName => "Parchment";
    public override string Description => "A sheet of scraped and whitened skin, ready for the pen";
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override int PriceReference => 6;
}

public sealed class Quill : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "quill";
    public override string DisplayName => "Quill";
    public override string Description => "A goose quill cut to a nib, its barbs stripped where the fingers hold it";
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override int PriceReference => 2;
}

public sealed class InkHorn : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "ink_horn";
    public override string DisplayName => "Ink Horn";
    public override string Description => "A cow's horn plugged with wax, half full of oak-gall ink gone thick at the bottom";
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 5;
}

public sealed class WaxTablet : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "wax_tablet";
    public override string DisplayName => "Wax Tablet";
    public override string Description => "A pair of boards hinged together, their wax faces scratched with sums and smoothed again";
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 4;
}

public sealed class Chalk : StoneRawItem
{
    public override string ItemId      => "chalk";
    public override string DisplayName => "Chalk";
    public override string Description => "A stick of soft white chalk, worn to a blunt point at one end";
    public override WeightClass Weight => WeightClass.Insignificant;
    public override int PriceReference => 1;
}

public sealed class Soap : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "soap";
    public override string DisplayName => "Soap";
    public override string Description => "A grey cake of tallow-and-ash soap that smells strongly of the lye in it";
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 3;
}

public sealed class Bandage : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "bandage";
    public override string DisplayName => "Bandage";
    public override string Description => "A rolled strip of boiled linen, clean and tightly wound";
    public override List<ItemTag> Tags => new() { ItemTag.Textile };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override int PriceReference => 3;
}

public sealed class Poultice : ConsumableItem
{
    public override string ItemId      => "poultice";
    public override string DisplayName => "Poultice";
    public override string Description => "A wad of bruised herbs and bran bound in a cloth, still faintly warm";
    public override List<ItemTag> Tags => new() { ItemTag.Herb };
    public override CoinType PriceCoin => CoinType.Copper;
    public override int PriceReference => 5;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PhlegmHumor>(40).Add<EtherHumor>(35).Add<FiberHumor>(25);
}

public sealed class Wine : ConsumableItem
{
    public override string ItemId      => "wine";
    public override string DisplayName => "Wine";
    public override string Description => "A stoppered jug of red wine, rough and strong";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override int PriceReference => 12;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Drink;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AlcoholHumor>(55).Add<SugarHumor>(25).Add<BloodHumor>(20);
}

public sealed class Incense : HerbItem
{
    public override string ItemId      => "incense";
    public override string DisplayName => "Incense";
    public override string Description => "A twist of paper holding grains of resin, sweet and smoky even unburnt";
    public override int PriceReference => 8;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<EtherHumor>(50).Add<FumeHumor>(30).Add<EuphoraHumor>(20);
}

public sealed class Manacles : Item
{
    public override ItemCategory Category => ItemCategory.Crafting;
    public override string ItemId      => "manacles";
    public override string DisplayName => "Manacles";
    public override string Description => "A pair of iron cuffs joined by a short chain, the hinges stiff with rust";
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Medium;
    public override int PriceReference => 14;
}
