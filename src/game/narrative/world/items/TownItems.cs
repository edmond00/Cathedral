using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative.World.Items;

// ─────────────────────────────────────────────────────────────────────────────
//  The goods of streets and harbours: what a merchant trades, what a sailor carries, what a tavern sells
//  across its counter and what a clerk writes with. Bought and sold in the towns and cities.
// ─────────────────────────────────────────────────────────────────────────────

// ── the market ──────────────────────────────────────────────────────────────

public sealed class DyedCloth : TextileItem
{
    public override string ItemId      => "dyed_cloth";
    public override string DisplayName => "Dyed Cloth";
    public override string Description => "A folded length of wool dyed a deep madder red";
    public override int PriceReference => 20;
}

public sealed class Sailcloth : TextileItem
{
    public override string ItemId      => "sailcloth";
    public override string DisplayName => "Sailcloth";
    public override string Description => "A heavy roll of tarred canvas, stiff and coarse";
    public override int PriceReference => 16;
}

public sealed class CottonCloth : TextileItem
{
    public override string ItemId      => "cotton_cloth";
    public override string DisplayName => "Cotton Cloth";
    public override string Description => "A bolt of plain white cotton, light and close-woven";
    public override int PriceReference => 14;
}

public sealed class Amphora : Item
{
    public override string ItemId      => "amphora";
    public override string DisplayName => "Amphora";
    public override string Description => "A tall two-handled clay jar sealed with pitch";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Large;
    public override WeightClass Weight => WeightClass.Heavy;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 6;
}

public sealed class SpiceBundle : Item
{
    public override string ItemId      => "spice_bundle";
    public override string DisplayName => "Spice Bundle";
    public override string Description => "A sewn cloth packet stamped with a merchant's mark, smelling of pepper";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Herb };
    public override int PriceReference => 30;
}

public sealed class Scales : Item
{
    public override string ItemId      => "scales";
    public override string DisplayName => "Merchant's Scales";
    public override string Description => "A folding brass balance with a set of nested weights";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 25;
}

public sealed class Abacus : Item
{
    public override string ItemId      => "abacus";
    public override string DisplayName => "Abacus";
    public override string Description => "A wooden counting frame strung with worn beads";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 10;
}

public sealed class Signet : Item
{
    public override string ItemId      => "signet";
    public override string DisplayName => "Signet Ring";
    public override string Description => "A heavy bronze ring cut with a merchant house's device";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 35;
}

public sealed class PromissoryNote : Item
{
    public override string ItemId      => "promissory_note";
    public override string DisplayName => "Promissory Note";
    public override string Description => "A folded slip promising payment, signed and sealed";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 20;
}

public sealed class Lock : Item
{
    public override string ItemId      => "padlock";
    public override string DisplayName => "Padlock";
    public override string Description => "A heavy iron padlock with its key on a thong";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork };
    public override int PriceReference => 12;
}

// ── the harbour ─────────────────────────────────────────────────────────────

public sealed class SaltFish : ConsumableItem
{
    public override string ItemId      => "salt_fish";
    public override string DisplayName => "Salt Fish";
    public override string Description => "A stiff slab of split, salted and dried fish";
    public override List<ItemTag> Tags => new() { ItemTag.Fish };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override bool IsHard => true;
    public override int PriceReference => 5;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SaltHumor>(50).Add<BloodHumor>(30).Add<AquaHumor>(20);
}

public sealed class Tar : Item
{
    public override string ItemId      => "tar";
    public override string DisplayName => "Tar";
    public override string Description => "A small pot of black pine tar for caulking";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Mineral };
    public override int PriceReference => 4;
}

public sealed class Oakum : Item
{
    public override string ItemId      => "oakum";
    public override string DisplayName => "Oakum";
    public override string Description => "A wad of old rope picked to fibres and tarred";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Textile };
    public override int PriceReference => 2;
}

public sealed class Marlinspike : Item
{
    public override string ItemId      => "marlinspike";
    public override string DisplayName => "Marlinspike";
    public override string Description => "A tapered iron spike for opening the strands of a rope";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override int UsageLevel => 4;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Ironwork };
    public override int PriceReference => 6;
}

public sealed class BelayingPin : Item
{
    public override string ItemId      => "belaying_pin";
    public override string DisplayName => "Belaying Pin";
    public override string Description => "A turned club of hard wood, a ship's rope-holder and a sailor's cosh";
    public override ItemCategory Category => ItemCategory.Tool;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override int UsageLevel => 2;
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Wood };
    public override int PriceReference => 3;
}

public sealed class Compass : Item
{
    public override string ItemId      => "compass";
    public override string DisplayName => "Compass";
    public override string Description => "A lodestone needle floating in a small brass bowl of water";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 45;
}

public sealed class Chart : Item
{
    public override string ItemId      => "chart";
    public override string DisplayName => "Sea Chart";
    public override string Description => "A sheepskin chart of coasts and soundings, salt-stained at the folds";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 30;
}

// ── the tavern ──────────────────────────────────────────────────────────────

public sealed class Pottage : ConsumableItem
{
    public override string ItemId      => "pottage";
    public override string DisplayName => "Pottage";
    public override string Description => "A bowl of thick pottage of beans, barley and whatever else there was";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 2;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PulpHumor>(50).Add<FiberHumor>(30).Add<AquaHumor>(20);
}

public sealed class MeatPie : ConsumableItem
{
    public override string ItemId      => "meat_pie";
    public override string DisplayName => "Meat Pie";
    public override string Description => "A crusted pie of minced mutton and onion, still warm";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 4;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(40).Add<FatHumor>(30).Add<PulpHumor>(30);
}

public sealed class Mead : ConsumableItem
{
    public override string ItemId      => "mead";
    public override string DisplayName => "Mead";
    public override string Description => "A jug of honey mead, golden and heady";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Drink;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 8;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AlcoholHumor>(50).Add<SugarHumor>(35).Add<LaetitiaHumor>(15);
}

public sealed class Beer : ConsumableItem
{
    public override string ItemId      => "beer";
    public override string DisplayName => "Hopped Beer";
    public override string Description => "A jug of bitter hopped beer, clearer than ale";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Drink;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override int PriceReference => 4;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AlcoholHumor>(45).Add<FumeHumor>(30).Add<ZenHumor>(25);
}

public sealed class Tankard : Item
{
    public override string ItemId      => "tankard";
    public override string DisplayName => "Pewter Tankard";
    public override string Description => "A lidded pewter tankard dented on one side";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 8;
}

public sealed class Dice : Item
{
    public override string ItemId      => "dice";
    public override string DisplayName => "Gaming Dice";
    public override string Description => "A pair of yellowed bone dice, one of them suspiciously heavy";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 2;
}

// ── the counting house ──────────────────────────────────────────────────────

public sealed class SealingWax : Item
{
    public override string ItemId      => "sealing_wax";
    public override string DisplayName => "Sealing Wax";
    public override string Description => "A stick of red sealing wax, one end melted and dripped";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 3;
}

public sealed class Letter : Item
{
    public override string ItemId      => "letter";
    public override string DisplayName => "Sealed Letter";
    public override string Description => "A folded letter closed with a blob of wax, addressed in a cramped hand";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 1;
}

public sealed class Ledgerbook : Item
{
    public override string ItemId      => "ledgerbook";
    public override string DisplayName => "Account Book";
    public override string Description => "A bound book of ruled pages, columns of figures in two inks";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 22;
}

public sealed class Scroll : Item
{
    public override string ItemId      => "scroll";
    public override string DisplayName => "Scroll";
    public override string Description => "A rolled sheet of parchment tied with a faded ribbon";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 8;
}

public sealed class Writ : Item
{
    public override string ItemId      => "writ";
    public override string DisplayName => "Writ";
    public override string Description => "A formal document bearing an official seal and a list of names";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 12;
}
