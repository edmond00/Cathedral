using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative.World.Items;

// ─────────────────────────────────────────────────────────────────────────────
//  What the stock keepers keep and handle: tack and tools for horses and herds, and the goods a byre or
//  a fold yields beyond the carcass. Body parts stay in BodyPartItems; this is the living trade.
// ─────────────────────────────────────────────────────────────────────────────

// ── tack and harness ────────────────────────────────────────────────────────

public sealed class Horseshoe : Item
{
    public override string ItemId      => "horseshoe";
    public override string DisplayName => "Horseshoe";
    public override string Description => "A worn iron horseshoe, the nail holes ragged";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork };
    public override int PriceReference => 4;
}

public sealed class Saddle : Item
{
    public override string ItemId      => "saddle";
    public override string DisplayName => "Saddle";
    public override string Description => "A leather saddle on a wooden tree, the seat polished by long riding";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Large;
    public override WeightClass Weight => WeightClass.Heavy;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 40;
}

public sealed class Bridle : Item
{
    public override string ItemId      => "bridle";
    public override string DisplayName => "Bridle";
    public override string Description => "A headstall and reins of oiled leather with an iron snaffle bit";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 18;
}

public sealed class HorseBlanket : Item
{
    public override string ItemId      => "horse_blanket";
    public override string DisplayName => "Horse Blanket";
    public override string Description => "A heavy striped wool blanket that smells strongly of horse";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Large;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Textile };
    public override int PriceReference => 12;
}

public sealed class Cowbell : Item
{
    public override string ItemId      => "cowbell";
    public override string DisplayName => "Cowbell";
    public override string Description => "A flat iron bell on a leather collar, its clapper a knuckle-bone";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork };
    public override int PriceReference => 5;
}

// ── stock tools ─────────────────────────────────────────────────────────────

public sealed class Currycomb : Item
{
    public override string ItemId      => "currycomb";
    public override string DisplayName => "Currycomb";
    public override string Description => "A rubbed iron comb with short teeth, for working the dust out of a coat";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override int UsageLevel => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 6;
}

public sealed class HoofPick : Item
{
    public override string ItemId      => "hoof_pick";
    public override string DisplayName => "Hoof Pick";
    public override string Description => "A short hooked iron spike for cleaning a hoof";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override int UsageLevel => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 3;
}

public sealed class Lariat : Item
{
    public override string ItemId      => "lariat";
    public override string DisplayName => "Lariat";
    public override string Description => "A long braided rawhide rope with a running noose at one end";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override int UsageLevel => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Craftware };
    public override int PriceReference => 10;
}

public sealed class BrandingIron : Item
{
    public override string ItemId      => "branding_iron";
    public override string DisplayName => "Branding Iron";
    public override string Description => "A long iron rod ending in a mark, blackened by fire";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override int UsageLevel => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 12;
}

public sealed class StockWhip : Item
{
    public override string ItemId      => "stock_whip";
    public override string DisplayName => "Stock Whip";
    public override string Description => "A long plaited leather whip on a short wooden handle";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override int UsageLevel => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Craftware };
    public override int PriceReference => 8;
}

public sealed class Pitchfork : Item
{
    public override string ItemId      => "pitchfork";
    public override string DisplayName => "Pitchfork";
    public override string Description => "A long-handled fork with two iron tines, for hay and dung";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Large;
    public override WeightClass Weight => WeightClass.Light;
    public override int UsageLevel => 3;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 10;
}

// ── what the fold yields ────────────────────────────────────────────────────

public sealed class Fleece : TextileItem
{
    public override string ItemId      => "fleece";
    public override string DisplayName => "Fleece";
    public override string Description => "A whole greasy fleece rolled up and tied, lanolin-sticky";
    public override int PriceReference => 14;
}

public sealed class Horsehair : TextileItem
{
    public override string ItemId      => "horsehair";
    public override string DisplayName => "Horsehair";
    public override string Description => "A hank of long coarse horsehair from a mane or tail";
    public override int PriceReference => 6;
}

public sealed class GoatMilk : ConsumableItem
{
    public override string ItemId      => "goat_milk";
    public override string DisplayName => "Goat Milk";
    public override string Description => "A skin of goat milk, rich and a little rank";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Drink;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 3;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(40).Add<AquaHumor>(35).Add<BloodHumor>(25);
}

public sealed class Ham : ConsumableItem
{
    public override string ItemId      => "ham";
    public override string DisplayName => "Ham";
    public override string Description => "A smoked ham in a cloth, its rind dark and hard";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 18;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(45).Add<BloodHumor>(30).Add<SaltHumor>(25);
}

public sealed class Bacon : ConsumableItem
{
    public override string ItemId      => "bacon";
    public override string DisplayName => "Bacon";
    public override string Description => "A slab of salted, smoked bacon streaked white and red";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 9;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(55).Add<SaltHumor>(30).Add<BloodHumor>(15);
}

public sealed class GooseEgg : ConsumableItem
{
    public override string ItemId      => "goose_egg";
    public override string DisplayName => "Goose Egg";
    public override string Description => "A heavy white goose egg, twice the size of a hen's";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 3;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(45).Add<PulpHumor>(35).Add<JuvenescenceHumor>(20);
}

public sealed class DuckEgg : ConsumableItem
{
    public override string ItemId      => "duck_egg";
    public override string DisplayName => "Duck Egg";
    public override string Description => "A pale blue-green duck egg, the shell faintly chalky";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 2;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(40).Add<PulpHumor>(40).Add<JuvenescenceHumor>(20);
}

public sealed class RabbitStew : ConsumableItem
{
    public override string ItemId      => "rabbit_stew";
    public override string DisplayName => "Rabbit Stew";
    public override string Description => "A covered pot of rabbit stewed with onion and thyme";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 6;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(40).Add<PulpHumor>(35).Add<LaetitiaHumor>(25);
}

public sealed class Curds : ConsumableItem
{
    public override string ItemId      => "curds";
    public override string DisplayName => "Curds";
    public override string Description => "A bowl of fresh white curds dripping whey";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 3;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(45).Add<PhlegmHumor>(35).Add<PulpHumor>(20);
}
