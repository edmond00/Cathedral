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
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Cloth });
    public override string ItemId      => "dyed_cloth";
    public override Craftsmanship Craftsmanship => Craftsmanship.Stock;
    public override List<ItemTag> Tags => new() { ItemTag.Textile, ItemTag.Cloth };
    public override string DisplayName => "Dyed Cloth";
    public override string Description => "A folded length of wool dyed a deep madder red";
}

public sealed class Sailcloth : TextileItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Cloth });
    public override string ItemId      => "sailcloth";
    public override Craftsmanship Craftsmanship => Craftsmanship.Stock;
    public override List<ItemTag> Tags => new() { ItemTag.Textile, ItemTag.Cloth };
    public override string DisplayName => "Sailcloth";
    public override string Description => "A heavy roll of tarred canvas, stiff and coarse";
}

public sealed class CottonCloth : TextileItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Cloth });
    public override string ItemId      => "cotton_cloth";
    public override Craftsmanship Craftsmanship => Craftsmanship.Stock;
    public override List<ItemTag> Tags => new() { ItemTag.Textile, ItemTag.Cloth };
    public override string DisplayName => "Cotton Cloth";
    public override string Description => "A bolt of plain white cotton, light and close-woven";
}

public sealed class Amphora : ContainerItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Vessel });
    public override string ItemId      => "amphora";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName => "Amphora";
    public override string Description => "A tall two-handled clay jar sealed with pitch";
    public override ItemSize Size => ItemSize.Large;
    public override WeightClass Weight => WeightClass.Heavy;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware, ItemTag.Pottery };
    public override ContainerKind Kind => ContainerKind.Vessel;
    public override int ContentSlots => 9;
}

public sealed class SpiceBundle : ConsumableItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Herb });
    public override string ItemId      => "spice_bundle";
    public override Rarity Rarity => Rarity.Scarce;
    public override string DisplayName => "Spice Bundle";
    public override string Description => "A sewn cloth packet stamped with a merchant's mark, smelling of pepper";
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Herb };
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe().Add<CholerHumor>(40).Add<VaporHumor>(35).Add<SaltHumor>(25);
}

public sealed class Scales : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Tool, ItemKind.Metal });
    public override string ItemId      => "scales";
    public override Craftsmanship Craftsmanship => Craftsmanship.Fine;
    public override string DisplayName => "Merchant's Scales";
    public override string Description => "A folding brass balance with a set of nested weights";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override IReadOnlyList<string> MadeForVerbIds => new[] { "propose_to_buy", "propose_to_sell" };
    public override int UsageLevel => 2;
}

public sealed class Abacus : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Tool });
    public override string ItemId      => "abacus";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName => "Abacus";
    public override string Description => "A wooden counting frame strung with worn beads";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware, ItemTag.Woodwork, ItemTag.Writing };
    public override IReadOnlyList<string> MadeForVerbIds => new[] { "propose_to_buy", "propose_to_sell" };
    public override int UsageLevel => 2;
}

public sealed class Signet : WearableItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Valuable });
    public override string ItemId      => "signet";
    public override Craftsmanship Craftsmanship => Craftsmanship.Fine;
    public override Rarity Rarity => Rarity.Rare;
    public override string DisplayName => "Signet Ring";
    public override string Description => "A heavy bronze ring cut with a merchant house's device";
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware, ItemTag.Writing };
    public override WearSlot Slot => WearSlot.Handwear;
    public override IReadOnlyList<Cathedral.Game.Npc.SocialCategory> DialogueAppeal => new[] { Cathedral.Game.Npc.SocialCategory.Bourgeois, Cathedral.Game.Npc.SocialCategory.Aristocrat };
    public override IReadOnlyList<string> MadeForVerbIds => new[] { "introduce_me" };
    public override int UsageLevel => 2;
}

public sealed class PromissoryNote : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Writing, ItemKind.Valuable });
    public override string ItemId      => "promissory_note";
    public override int? PriceOverride => 20;
    public override string DisplayName => "Promissory Note";
    public override string Description => "A folded slip promising payment, signed and sealed";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware, ItemTag.Writing };
    public override IReadOnlyList<string> MadeForVerbIds => new[] { "offer_bribe", "propose_to_buy" };
    public override int UsageLevel => 2;
}

public sealed class Lock : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Metal });
    public override string ItemId      => "padlock";
    public override string DisplayName => "Padlock";
    public override string Description => "A heavy iron padlock with its key on a thong";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Ironwork };
}

// ── the harbour ─────────────────────────────────────────────────────────────

public sealed class SaltFish : ConsumableItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Fish });
    public override string ItemId      => "salt_fish";
    public override Craftsmanship Craftsmanship => Craftsmanship.Stock;
    public override string DisplayName => "Salt Fish";
    public override string Description => "A stiff slab of split, salted and dried fish";
    public override List<ItemTag> Tags => new() { ItemTag.Fish };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    public override bool IsHard => true;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SaltHumor>(50).Add<BloodHumor>(30).Add<AquaHumor>(20);
}

public sealed class Tar : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Fuel });
    public override string ItemId      => "tar";
    public override string DisplayName => "Tar";
    public override string Description => "A small pot of black pine tar for caulking";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Mineral, ItemTag.Fuel };
}

public sealed class Oakum : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Fibre });
    public override string ItemId      => "oakum";
    public override Craftsmanship Craftsmanship => Craftsmanship.Stock;
    public override string DisplayName => "Oakum";
    public override string Description => "A wad of old rope picked to fibres and tarred";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Textile };
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
    public override List<ItemTag> Tags => new() { ItemTag.Tool, ItemTag.Wood, ItemTag.Woodwork };
}

public sealed class Compass : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Tool });
    public override string ItemId      => "compass";
    public override Craftsmanship Craftsmanship => Craftsmanship.Fine;
    public override Rarity Rarity => Rarity.Rare;
    public override string DisplayName => "Compass";
    public override string Description => "A lodestone needle floating in a small brass bowl of water";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override IReadOnlyList<string> MadeForVerbIds => new[] { "voyage_toward", "follow_path", "backtrack" };
    public override int UsageLevel => 2;
}

public sealed class Chart : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Writing });
    public override string ItemId      => "chart";
    public override Craftsmanship Craftsmanship => Craftsmanship.Fine;
    public override Rarity Rarity => Rarity.Rare;
    public override string DisplayName => "Sea Chart";
    public override string Description => "A sheepskin chart of coasts and soundings, salt-stained at the folds";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override IReadOnlyList<string> MadeForVerbIds => new[] { "voyage_toward", "follow_path", "backtrack" };
    public override int UsageLevel => 2;
}

// ── the tavern ──────────────────────────────────────────────────────────────

public sealed class Pottage : ConsumableItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Food });
    public override string ItemId      => "pottage";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName => "Pottage";
    public override string Description => "A bowl of thick pottage of beans, barley and whatever else there was";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<PulpHumor>(50).Add<FiberHumor>(30).Add<AquaHumor>(20);
}

public sealed class MeatPie : ConsumableItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Meat, ItemKind.Bread });
    public override string ItemId      => "meat_pie";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName => "Meat Pie";
    public override string Description => "A crusted pie of minced mutton and onion, still warm";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff, ItemTag.Bakery, ItemTag.Meat };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Food;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<BloodHumor>(40).Add<FatHumor>(30).Add<PulpHumor>(30);
}

public sealed class Mead : ConsumableItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Honey, ItemKind.Liquor });
    public override string ItemId      => "mead";
    public override Craftsmanship Craftsmanship => Craftsmanship.Stock;
    public override string DisplayName => "Mead";
    public override string Description => "A jug of honey mead, golden and heady";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff, ItemTag.Ale };
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Drink;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AlcoholHumor>(50).Add<SugarHumor>(35).Add<LaetitiaHumor>(15);
}

public sealed class Beer : ConsumableItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Liquor });
    public override string ItemId      => "beer";
    public override Craftsmanship Craftsmanship => Craftsmanship.Stock;
    public override string DisplayName => "Hopped Beer";
    public override string Description => "A jug of bitter hopped beer, clearer than ale";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff, ItemTag.Ale };
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override ConsumableType ConsumableType => ConsumableType.Drink;
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AlcoholHumor>(45).Add<FumeHumor>(30).Add<ZenHumor>(25);
}

public sealed class Tankard : ContainerItem
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Vessel });
    public override string ItemId      => "tankard";
    public override string DisplayName => "Pewter Tankard";
    public override string Description => "A lidded pewter tankard dented on one side";
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override ContainerKind Kind => ContainerKind.Vessel;
    public override int ContentSlots => 3;
}

public sealed class Dice : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Bone });
    public override string ItemId      => "dice";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName => "Gaming Dice";
    public override string Description => "A pair of yellowed bone dice, one of them suspiciously heavy";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override IReadOnlyList<string> MadeForVerbIds => new[] { "play" };
    public override int UsageLevel => 2;
}

// ── the counting house ──────────────────────────────────────────────────────

public sealed class SealingWax : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Writing });
    public override string ItemId      => "sealing_wax";
    public override Craftsmanship Craftsmanship => Craftsmanship.Plain;
    public override string DisplayName => "Sealing Wax";
    public override string Description => "A stick of red sealing wax, one end melted and dripped";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware, ItemTag.Chandlery, ItemTag.Writing };
}

public sealed class Letter : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Writing });
    public override string ItemId      => "letter";
    public override Craftsmanship Craftsmanship => Craftsmanship.Fine;
    public override string DisplayName => "Sealed Letter";
    public override string Description => "A folded letter closed with a blob of wax, addressed in a cramped hand";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware, ItemTag.Writing };
    public override IReadOnlyList<string> MadeForVerbIds => new[] { "petition", "explain_myself", "introduce_me" };
    public override int UsageLevel => 2;
}

public sealed class Ledgerbook : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Book, ItemKind.Writing });
    public override string ItemId      => "ledgerbook";
    public override Craftsmanship Craftsmanship => Craftsmanship.Fine;
    public override string DisplayName => "Account Book";
    public override string Description => "A bound book of ruled pages, columns of figures in two inks";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware, ItemTag.Writing };
    public override IReadOnlyList<string> MadeForVerbIds => new[] { "propose_to_buy", "propose_to_sell" };
    public override int UsageLevel => 2;
}

public sealed class Scroll : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Writing });
    public override string ItemId      => "scroll";
    public override Craftsmanship Craftsmanship => Craftsmanship.Fine;
    public override string DisplayName => "Scroll";
    public override string Description => "A rolled sheet of parchment tied with a faded ribbon";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware, ItemTag.Writing };
    public override IReadOnlyList<string> MadeForVerbIds => new[] { "gather_knowledge", "ask_teaching", "examine" };
    public override int UsageLevel => 2;
}

public sealed class Writ : Item
{
    public override System.Collections.Generic.IEnumerable<ItemKind> DeclaredKinds => System.Linq.Enumerable.Concat(base.DeclaredKinds, new[] { ItemKind.Writing });
    public override string ItemId      => "writ";
    public override Craftsmanship Craftsmanship => Craftsmanship.Fine;
    public override string DisplayName => "Writ";
    public override string Description => "A formal document bearing an official seal and a list of names";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware, ItemTag.Writing };
    public override IReadOnlyList<string> MadeForVerbIds => new[] { "petition", "explain_myself", "introduce_me" };
    public override int UsageLevel => 2;
}
