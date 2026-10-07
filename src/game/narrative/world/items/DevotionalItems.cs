using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative.World.Items;

// ─────────────────────────────────────────────────────────────────────────────
//  What temples, monasteries and the dead keep: the instruments of worship, the books of the schools,
//  and what the old burial grounds and ruins still give up to a careful hand.
// ─────────────────────────────────────────────────────────────────────────────

// ── worship ─────────────────────────────────────────────────────────────────

public sealed class Censer : Item
{
    public override string ItemId      => "censer";
    public override string DisplayName => "Censer";
    public override string Description => "A bronze censer on three chains, black inside with old resin";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 30;
}

public sealed class HolyWater : ConsumableItem
{
    public override string ItemId      => "holy_water";
    public override string DisplayName => "Blessed Water";
    public override string Description => "A small stoppered vial of water drawn and blessed at a font";
    public override List<ItemTag> Tags => new() { ItemTag.Foodstuff };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override ConsumableType ConsumableType => ConsumableType.Drink;
    protected override HumorRichness Richness => HumorRichness.Sparse;
    public override int PriceReference => 6;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<AquaHumor>(50).Add<ZenHumor>(30).Add<EtherHumor>(20);
}

public sealed class Relic : Item
{
    public override string ItemId      => "relic";
    public override string DisplayName => "Relic";
    public override string Description => "A sliver of yellowed bone in a crystal case bound with silver wire";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 80;
}

public sealed class Icon : Item
{
    public override string ItemId      => "icon";
    public override string DisplayName => "Painted Icon";
    public override string Description => "A small panel painted with a stern gold-haloed face";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 40;
}

public sealed class PrayerBook : Item
{
    public override string ItemId      => "prayer_book";
    public override string DisplayName => "Book of Hours";
    public override string Description => "A small book of prayers, its margins crowded with painted birds";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 50;
}

public sealed class VotiveCandle : Item
{
    public override string ItemId      => "votive_candle";
    public override string DisplayName => "Votive Candle";
    public override string Description => "A fat beeswax candle stamped with a holy sign";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 3;
}

public sealed class Aspergillum : Item
{
    public override string ItemId      => "aspergillum";
    public override string DisplayName => "Aspergillum";
    public override string Description => "A short silver rod ending in a pierced ball, for sprinkling water";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 25;
}

public sealed class Offering : Item
{
    public override string ItemId      => "offering";
    public override string DisplayName => "Offering Bowl";
    public override string Description => "A shallow clay bowl holding grain and a copper coin";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 2;
}

public sealed class Ampulla : Item
{
    public override string ItemId      => "ampulla";
    public override string DisplayName => "Pilgrim's Ampulla";
    public override string Description => "A small lead flask stamped with a shrine's image, worn on a cord";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 4;
}

// ── the schools ─────────────────────────────────────────────────────────────

public sealed class Primer : Item
{
    public override string ItemId      => "primer";
    public override string DisplayName => "Primer";
    public override string Description => "A thin book of letters and simple verses, much thumbed";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 12;
}

public sealed class Astrolabe : Item
{
    public override string ItemId      => "astrolabe";
    public override string DisplayName => "Astrolabe";
    public override string Description => "A brass disc engraved with stars and lines, a pointer pivoting at its centre";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 90;
}

public sealed class Pen : Item
{
    public override string ItemId      => "pen";
    public override string DisplayName => "Reed Pen";
    public override string Description => "A cut reed pen, its nib split and stained black";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 1;
}

public sealed class Rod : Item
{
    public override string ItemId      => "rod";
    public override string DisplayName => "Master's Rod";
    public override string Description => "A thin birch rod, worn smooth at the grip";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Wood };
    public override int PriceReference => 1;
}

public sealed class Diagram : Item
{
    public override string ItemId      => "diagram";
    public override string DisplayName => "Diagram";
    public override string Description => "A sheet inked with circles, angles and careful lettering";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 6;
}

public sealed class Seal : Item
{
    public override string ItemId      => "seal";
    public override string DisplayName => "Imperial Seal";
    public override string Description => "A heavy seal-matrix of bronze, the empire's device reversed in it";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 60;
}

// ── the dead and the ruined ─────────────────────────────────────────────────

public sealed class Potsherd : StoneRawItem
{
    public override string ItemId      => "potsherd";
    public override string DisplayName => "Potsherd";
    public override string Description => "A curved fragment of painted pottery, the pattern half worn away";
    public override int PriceReference => 1;
}

public sealed class OldCoin : Item
{
    public override string ItemId      => "old_coin";
    public override string DisplayName => "Corroded Coin";
    public override string Description => "A green-crusted coin of a mint nobody uses any more";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Mineral };
    public override int PriceReference => 8;
}

public sealed class Idol : Item
{
    public override string ItemId      => "idol";
    public override string DisplayName => "Broken Idol";
    public override string Description => "A small stone figure, its head long since knocked off";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Mineral };
    public override int PriceReference => 15;
}

public sealed class MosaicTile : StoneRawItem
{
    public override string ItemId      => "mosaic_tile";
    public override string DisplayName => "Mosaic Tile";
    public override string Description => "A small square of blue glazed stone from a broken floor";
    public override int PriceReference => 2;
}

public sealed class DeathMask : Item
{
    public override string ItemId      => "death_mask";
    public override string DisplayName => "Funerary Mask";
    public override string Description => "A thin beaten mask of gilded copper, eyes closed";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 70;
}

public sealed class GraveRing : Item
{
    public override string ItemId      => "grave_ring";
    public override string DisplayName => "Grave Ring";
    public override string Description => "A plain gold ring dull with soil";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 45;
}

public sealed class Scarab : Item
{
    public override string ItemId      => "scarab";
    public override string DisplayName => "Scarab Amulet";
    public override string Description => "A carved stone beetle with signs cut into its flat underside";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 25;
}

public sealed class BurialUrn : Item
{
    public override string ItemId      => "burial_urn";
    public override string DisplayName => "Burial Urn";
    public override string Description => "A lidded urn of black clay, ash still inside it";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Medium;
    public override WeightClass Weight => WeightClass.Medium;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware };
    public override int PriceReference => 10;
}

public sealed class Shroud : Item
{
    public override string ItemId      => "shroud";
    public override string DisplayName => "Shroud";
    public override string Description => "A length of yellowed linen, folded, smelling of earth and myrrh";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Light;
    public override List<ItemTag> Tags => new() { ItemTag.Textile };
    public override int PriceReference => 6;
}

public sealed class Inscription : StoneRawItem
{
    public override string ItemId      => "inscription";
    public override string DisplayName => "Inscribed Stone";
    public override string Description => "A broken slab with lines of a script nobody reads now";
    public override int PriceReference => 12;
}
