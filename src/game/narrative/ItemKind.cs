using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// What an item <i>is</i>, as a family a will can want: "a mushroom", "a blade", "something holy".
/// An item carries every kind that fairly names it (<see cref="Item.Kinds"/>), and a will target
/// narrowed by kind (<see cref="WillTarget.YieldingKind"/>, <see cref="WillTarget.Selling"/>) is
/// answered by all of them — mycology wants a mushroom, and the field mushroom, the desert truffle and
/// the lichen all answer.
///
/// <para><b>Separate from <see cref="ItemTag"/> on purpose.</b> A trade tag is a shelf: a catalogue is a
/// draw of about five items from the whole tag (see its summary), so every item added to a tag can turn
/// up for sale at every trader of it. A kind is meant to be generous — a hazelnut is a nut, a fruit,
/// something foraged and something to eat — and tagging that generously in the trade taxonomy would have
/// restocked every shelf in the game. The two answer different questions: who sells it, and what it is.</para>
///
/// <para>An enum, never a string, for the reason <c>CLAUDE.md</c> gives: a will that named a kind no
/// item carried would refuse in every place and nothing would notice. <c>--item-audit</c> counts the
/// items of each kind and lists the items that carry none.</para>
/// </summary>
public enum ItemKind
{
    // ── What grows ─────────────────────────────────────────────────────
    Mushroom, Herb, Flower, Plant, Berry, Fruit, Nut, Vegetable, Grain, Seed, Fodder,
    // ── What is taken from beasts ──────────────────────────────────────
    Egg, Honey, Fish, Shellfish, Meat, Dairy, Hide, Bone, Ivory, Feather,
    // ── What is eaten and drunk ────────────────────────────────────────
    Food, Bread, Drink, Liquor, Remedy,
    // ── What is made and used ──────────────────────────────────────────
    Tool, Weapon, Blade, Axe, Spear, Bow, Bludgeon, Shield, Armour, Garment, Vessel, Light, Writing, Book,
    // ── What the earth gives ───────────────────────────────────────────
    Ore, Metal, Stone, Gem, Wood, Fuel, Fibre, Cloth,
    // ── What is kept and prized ────────────────────────────────────────
    Holy, Relic, Valuable, Flotsam,
}

/// <summary>Reading an item's kinds once per item class.</summary>
internal static class ItemKinds
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyCollection<ItemKind>> _byType = new();

    /// <summary>
    /// An item's declared kinds plus those its category implies, so no weapon, tool, garment, vessel,
    /// food or drink is ever left out of its broadest family. Cached by class: kinds never depend on state.
    /// </summary>
    public static IReadOnlyCollection<ItemKind> Of(Item item) => _byType.GetOrAdd(item.GetType(), _ =>
    {
        var kinds = new HashSet<ItemKind>(item.DeclaredKinds);
        switch (item.Category)
        {
            case ItemCategory.Weapon:    kinds.Add(ItemKind.Weapon); break;
            case ItemCategory.Tool:      kinds.Add(ItemKind.Tool); break;
            case ItemCategory.Wearing:   kinds.Add(ItemKind.Garment); break;
            case ItemCategory.Container: kinds.Add(ItemKind.Vessel); break;
        }
        if (item is ConsumableItem c)
            kinds.Add(c.ConsumableType == ConsumableType.Drink ? ItemKind.Drink : ItemKind.Food);
        if (item is WearableItem { DefenseDice: > 0 }) kinds.Add(ItemKind.Armour);
        return kinds.OrderBy(k => k).ToArray();
    });
}
