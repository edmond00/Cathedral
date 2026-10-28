using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Narrative.World.Items;

// ─────────────────────────────────────────────────────────────────────────────
//  What the new kinds of wild place hold (see WildAreaKinds.cs): the forage of a saltmarsh or a
//  wellspring, the curiosities of a cave or a barrow, the stones a desert or a canyon gives up. Built
//  on the same bases as everything else, so samphire is a vegetable and a cave pearl is stone to
//  every rule that asks.
// ─────────────────────────────────────────────────────────────────────────────

// ── Caves ───────────────────────────────────────────────────────────────────

/// <summary>Bat dung dug from a colony floor — the best manure there is, and sold as such.</summary>
public sealed class Guano : Item
{
    public override string ItemId      => "guano";
    public override string DisplayName => "Guano";
    public override string Description => "A double handful of dry black bat dung, rank and crumbling, worth its weight to any gardener";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override List<ItemTag> Tags => new() { ItemTag.Mineral, ItemTag.Fodder };
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 3;
}

public sealed class CavePearl : StoneRawItem
{
    public override string ItemId      => "cave_pearl";
    public override string DisplayName => "Cave Pearl";
    public override string Description => "A smooth white ball of stone grown layer on layer in a dripping pool, like a pearl without an oyster";
    public override List<ItemTag> Tags => new() { ItemTag.Mineral, ItemTag.Ore };
    public override WeightClass Weight => WeightClass.Insignificant;
    public override int PriceReference => 15;
}

public sealed class RockCrystal : StoneRawItem
{
    public override string ItemId      => "rock_crystal";
    public override string DisplayName => "Rock Crystal";
    public override string Description => "A six-sided spar of clear crystal, cold as ice and as clear, broken from a cluster";
    public override List<ItemTag> Tags => new() { ItemTag.Mineral, ItemTag.Ore };
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 20;
}

// ── Coast ───────────────────────────────────────────────────────────────────

public sealed class Samphire : VegetableItem
{
    public override string ItemId      => "samphire";
    public override string DisplayName => "Samphire";
    public override string Description => "A bunch of glassy green samphire from the saltmarsh, crisp and salt as the sea";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SaltHumor>(45).Add<AquaHumor>(35).Add<FiberHumor>(20);
}

public sealed class Cockle : SeaFoodItem
{
    public override string ItemId      => "cockle";
    public override string DisplayName => "Cockle";
    public override string Description => "A handful of ribbed cockles raked out of the wet sand, shut tight";
    protected override HumorRichness Richness => HumorRichness.Modest;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SaltHumor>(40).Add<BloodHumor>(35).Add<CalxHumor>(25);
}

public sealed class Amber : StoneRawItem
{
    public override string ItemId      => "amber";
    public override string DisplayName => "Amber";
    public override string Description => "A lump of honey-coloured amber thrown up on the shingle, warm to the hand, a gnat caught in it";
    public override List<ItemTag> Tags => new() { ItemTag.Mineral, ItemTag.Ore };
    public override WeightClass Weight => WeightClass.Insignificant;
    public override int PriceReference => 18;
}

// ── Woods and water ─────────────────────────────────────────────────────────

public sealed class Hazelnut : FruitItem
{
    public override string ItemId      => "hazelnut";
    public override string DisplayName => "Hazelnut";
    public override string Description => "A handful of hazelnuts in their ragged green husks, sweet once cracked";
    protected override HumorRichness Richness => HumorRichness.Modest;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FatHumor>(45).Add<PulpHumor>(35).Add<FiberHumor>(20);
}

public sealed class Meadowsweet : HerbItem
{
    public override string ItemId      => "meadowsweet";
    public override string DisplayName => "Meadowsweet";
    public override string Description => "A frothy cream head of meadowsweet from a wet meadow, smelling of almonds and honey";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<ZenHumor>(40).Add<SugarHumor>(30).Add<AquaHumor>(30);
}

public sealed class BogMyrtle : HerbItem
{
    public override string ItemId      => "bog_myrtle";
    public override string DisplayName => "Bog Myrtle";
    public override string Description => "A sprig of bog myrtle from the fen, resinous and sharp, good against midges and for bittering ale";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<EtherHumor>(40).Add<CholerHumor>(35).Add<FiberHumor>(25);
}

// ── Old ground ──────────────────────────────────────────────────────────────

/// <summary>A flint arrowhead of the old people, turned up where barrows stand. Country folk call it elf-shot.</summary>
public sealed class ElfShot : Item
{
    public override string ItemId      => "elf_shot";
    public override string DisplayName => "Elf-Shot";
    public override string Description => "A leaf-shaped arrowhead of knapped flint, finer than any smith could make, turned up from old ground";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware, ItemTag.Devotional };
    public override ItemSize Size => ItemSize.Small;
    public override WeightClass Weight => WeightClass.Insignificant;
    public override int PriceReference => 9;
}

// ── Dry country ─────────────────────────────────────────────────────────────

public sealed class DesertTruffle : VegetableItem
{
    public override string ItemId      => "desert_truffle";
    public override string DisplayName => "Desert Truffle";
    public override string Description => "A pale knobbly truffle dug from under a cracked mound of sand, nutty and earthy";
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<FungiHumor>(50).Add<PulpHumor>(30).Add<CalxHumor>(20);
}

public sealed class Agate : StoneRawItem
{
    public override string ItemId      => "agate";
    public override string DisplayName => "Agate";
    public override string Description => "A rough nodule of agate split open on banded rings of grey, white and rust";
    public override List<ItemTag> Tags => new() { ItemTag.Mineral, ItemTag.Ore };
    public override WeightClass Weight => WeightClass.Light;
    public override int PriceReference => 14;
}

// ── Cold country ────────────────────────────────────────────────────────────

/// <summary>Old tusk ivory the frozen ground gives back where a river cuts a bank.</summary>
public sealed class FossilIvory : Item
{
    public override string ItemId      => "fossil_ivory";
    public override string DisplayName => "Fossil Ivory";
    public override string Description => "A curved length of yellowed ivory from a beast far bigger than any living, thawed out of a riverbank";
    public override ItemCategory Category => ItemCategory.Crafting;
    public override List<ItemTag> Tags => new() { ItemTag.Craftware, ItemTag.Mineral };
    public override WeightClass Weight => WeightClass.Medium;
    public override int PriceReference => 30;
}

public sealed class Lingonberry : FruitItem
{
    public override string ItemId      => "lingonberry";
    public override string DisplayName => "Lingonberry";
    public override string Description => "A handful of hard red lingonberries off the low evergreen mat, tart enough to make the eyes water";
    protected override HumorRichness Richness => HumorRichness.Sparse;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<CholerHumor>(40).Add<SugarHumor>(35).Add<AquaHumor>(25);
}

// ── Wild bees ───────────────────────────────────────────────────────────────

/// <summary>Comb cut from a wild hive in a tree or a rock — robbed, not kept.</summary>
public sealed class WildHoney : FruitItem
{
    public override string ItemId      => "wild_honey";
    public override string DisplayName => "Wild Honeycomb";
    public override string Description => "A dripping slab of dark wild honeycomb, grubs in some of the cells, sweet enough to hurt";
    protected override HumorRichness Richness => HumorRichness.Hearty;
    protected override HumorRecipe Recipe => new HumorRecipe()
        .Add<SugarHumor>(60).Add<LaetitiaHumor>(25).Add<AquaHumor>(15);
}
