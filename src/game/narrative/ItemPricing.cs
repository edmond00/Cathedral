using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative.Crafting;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// How much work stands between an item and the world it came from. A multiplier on what the item is
/// made of, plus a flat charge for the labour (<see cref="ItemPricing"/>).
/// </summary>
public enum Craftsmanship
{
    /// <summary>Picked up, gathered, cut or dug: a log, an ore, a turnip, a feather.</summary>
    Raw,
    /// <summary>A material worked once into stock: an iron bar, a bolt of linen, flour, tallow, ale.</summary>
    Stock,
    /// <summary>A thing made for use: a knife, a tunic, a bucket, a loaf.</summary>
    Plain,
    /// <summary>A thing made well, for those who can pay for it: a doublet, a censer, a ham.</summary>
    Fine,
    /// <summary>A master's piece that few hands in the world could make: an astrolabe.</summary>
    Masterwork,
}

/// <summary>
/// How hard an item is to come by, whatever it is made of — the part of a price that matter and work
/// do not explain. Added on top, never multiplied, so a heavy rare thing does not run away.
/// </summary>
public enum Rarity
{
    Common,
    /// <summary>Not found everywhere: a jasper, an old coin, olive oil away from the olive.</summary>
    Uncommon,
    /// <summary>Brought from far off, or seldom found: spices, salt, amber, a fossil.</summary>
    Scarce,
    /// <summary>Owned by few: a relic, a signet, a compass.</summary>
    Rare,
    /// <summary>Coveted by kings: a diamond. Nothing is this precious yet.</summary>
    Precious,
}

/// <summary>
/// The price of every item, worked out from what the item is rather than written beside it.
///
/// <code>price = matter × craftsmanship + labour + function + rarity     (or an explicit override)</code>
///
/// <list type="bullet">
///   <item><b>matter</b> — what the item holds. An item with crafting materials holds
///     <see cref="ItemMaterials.Units(Item)"/> units of each, priced at its dearest material's
///     <see cref="UnitValue"/>. Anything else (food, a feather, a letter) is priced by its dearest
///     <see cref="ItemKind"/> (<see cref="KindValue"/>), doubled for a medium burden and quadrupled for a
///     heavy one.</item>
///   <item><b>craftsmanship</b> — <see cref="Item.Craftsmanship"/>: a multiplier on matter and a flat
///     labour charge.</item>
///   <item><b>function</b> — what the item does: a weapon's level above a bare fist, an armour's dice,
///     a tool's usage level, a container's room.</item>
///   <item><b>rarity</b> — <see cref="Item.Rarity"/>, added.</item>
/// </list>
///
/// <para><b>Breaking down never pays.</b> Craftsmanship never multiplies matter by less than one, so an
/// item is always worth at least the units of any material it gives back at a station. And a refined
/// material is always dearer by the unit than the raw one it is made from, so refining is worth doing.
/// <c>--item-audit</c> checks both, and fails any price above <see cref="MaxPrice"/>.</para>
/// </summary>
public static class ItemPricing
{
    /// <summary>The highest reference price. A merchant's variation on top is clamped to 100.</summary>
    public const int MaxPrice = 90;

    /// <summary>Copper per unit of each crafting material. A refined material is dearer than its raw one.</summary>
    public static double UnitValue(CraftMaterial m) => m switch
    {
        CraftMaterial.Stone   => 0.25,
        CraftMaterial.Clay    => 0.5,
        CraftMaterial.Wood    => 1,
        CraftMaterial.Fibre   => 1,
        CraftMaterial.Bone    => 1,
        CraftMaterial.Ore     => 1.5,
        CraftMaterial.Fat     => 1.5,
        CraftMaterial.Hide    => 2,
        CraftMaterial.Metal   => 2.5,
        CraftMaterial.Cloth   => 3,
        CraftMaterial.Leather => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(m), m, null),
    };

    /// <summary>
    /// Copper for one portion of something with no crafting material, by what it is. A kind absent from
    /// this table (Food, Tool…) says nothing about value; an item whose kinds are all absent is worth one.
    /// </summary>
    public static double? KindValue(ItemKind k) => k switch
    {
        ItemKind.Plant or ItemKind.Flower or ItemKind.Egg or ItemKind.Feather
            or ItemKind.Drink or ItemKind.Writing                             => 1,
        ItemKind.Herb or ItemKind.Fodder or ItemKind.Seed or ItemKind.Fuel
            or ItemKind.Liquor                                                => 2,
        ItemKind.Fruit or ItemKind.Berry or ItemKind.Nut or ItemKind.Vegetable
            or ItemKind.Grain or ItemKind.Mushroom or ItemKind.Dairy
            or ItemKind.Bread or ItemKind.Remedy                              => 3,
        ItemKind.Fish or ItemKind.Shellfish or ItemKind.Honey                 => 4,
        ItemKind.Meat                                                         => 5,
        _                                                                     => null,
    };

    public static double Multiplier(Craftsmanship c) => c switch
    {
        Craftsmanship.Raw        => 1,
        Craftsmanship.Stock      => 1.5,
        Craftsmanship.Plain      => 2,
        Craftsmanship.Fine       => 4,
        Craftsmanship.Masterwork => 5,
        _ => throw new ArgumentOutOfRangeException(nameof(c), c, null),
    };

    public static int Labour(Craftsmanship c) => c switch
    {
        Craftsmanship.Raw        => 0,
        Craftsmanship.Stock      => 1,
        Craftsmanship.Plain      => 2,
        Craftsmanship.Fine       => 4,
        Craftsmanship.Masterwork => 5,
        _ => throw new ArgumentOutOfRangeException(nameof(c), c, null),
    };

    public static int Premium(Rarity r) => r switch
    {
        Rarity.Common   => 0,
        Rarity.Uncommon => 5,
        Rarity.Scarce   => 12,
        Rarity.Rare     => 30,
        Rarity.Precious => 60,
        _ => throw new ArgumentOutOfRangeException(nameof(r), r, null),
    };

    /// <summary>Copper per level a weapon adds above a trained fist.</summary>
    public const int PerWeaponLevel = 8;
    /// <summary>Copper per die of armour.</summary>
    public const int PerDefenseDie = 12;
    /// <summary>Copper per usage level above the first, for a tool.</summary>
    public const int PerUsageLevel = 2;
    /// <summary>Copper per slot of room in a container.</summary>
    public const double PerContentSlot = 0.25;

    /// <summary>The parts a price is summed from, for the audit and the item's own price.</summary>
    public readonly record struct Breakdown(
        double Matter, Craftsmanship Craftsmanship, double Function, Rarity Rarity, int? Override)
    {
        /// <summary>The sum before rounding and the cap.</summary>
        public double Raw => Override ?? Matter * Multiplier(Craftsmanship) + Labour(Craftsmanship) + Function + Premium(Rarity);

        /// <summary>The reference price: the sum rounded, at least one, at most <see cref="MaxPrice"/>.</summary>
        public int Price => Math.Clamp((int)Math.Round(Raw, MidpointRounding.AwayFromZero), 1, MaxPrice);
    }

    private static readonly ConcurrentDictionary<Type, Breakdown> _byType = new();

    /// <summary>The reference price of <paramref name="item"/>. Cached by class: a price never depends on state.</summary>
    public static int Price(Item item) => Of(item).Price;

    /// <summary>How <paramref name="item"/>'s price is made up.</summary>
    public static Breakdown Of(Item item) => _byType.GetOrAdd(item.GetType(), _ => new Breakdown(
        Matter(item), item.Craftsmanship, Function(item), item.Rarity, item.PriceOverride));

    /// <summary>What the item is made of, in copper, before any work.</summary>
    public static double Matter(Item item)
    {
        var materials = ItemMaterials.Of(item);
        if (materials.Count > 0)
            return ItemMaterials.Units(item) * materials.Max(UnitValue);

        double perPortion = item.Kinds.Select(KindValue).Where(v => v.HasValue).Select(v => v!.Value)
                                      .DefaultIfEmpty(1).Max();
        return perPortion * Portions(item.Weight);
    }

    /// <summary>How many portions a thing without materials is: one, two when medium, four when heavy.</summary>
    public static int Portions(WeightClass weight) => Math.Max(1, ItemMaterials.Units(weight) / 2);

    /// <summary>What the item does, in copper.</summary>
    public static double Function(Item item)
    {
        double f = 0;
        if (item is WeaponItem { IsImprovised: false } w)
            f += Math.Max(0, w.Level - WeaponItem.StrikingLimbCeiling) * PerWeaponLevel;
        if (item is WearableItem { DefenseDice: > 0 } armour)
            f += armour.DefenseDice * PerDefenseDie;
        if (item.Category == ItemCategory.Tool)
            f += Math.Max(0, item.UsageLevel - 1) * PerUsageLevel;
        if (item is IContainer c)
            f += c.ContentSlots * PerContentSlot;
        return f;
    }

    /// <summary>
    /// The craftsmanship an item has unless it says otherwise (<see cref="Item.Craftsmanship"/>):
    /// anything eaten or drunk, anything of no crafting material, and stock or goods of raw materials
    /// only are <see cref="Craftsmanship.Raw"/>; a garment only an aristocrat would wear is
    /// <see cref="Craftsmanship.Fine"/> and one only a pauper would is barely worked
    /// (<see cref="Craftsmanship.Stock"/>); everything else is
    /// <see cref="Craftsmanship.Plain"/>. An item that is stock, or made by hand from raw materials,
    /// declares it.
    /// </summary>
    public static Craftsmanship DefaultCraftsmanship(Item item)
    {
        if (item is ConsumableItem) return Craftsmanship.Raw;

        var materials = ItemMaterials.Of(item);
        if (materials.Count == 0) return Craftsmanship.Raw;

        if (item.Category is ItemCategory.Crafting or ItemCategory.Other
            && materials.All(m => !m.IsRefined()))
            return Craftsmanship.Raw;

        if (item is WearableItem { DialogueAppeal.Count: > 0 } garment)
        {
            if (garment.DialogueAppeal.All(a => a == SocialCategory.Aristocrat)) return Craftsmanship.Fine;
            if (garment.DialogueAppeal.All(a => a == SocialCategory.Pauper))     return Craftsmanship.Stock;
        }
        return Craftsmanship.Plain;
    }
}
