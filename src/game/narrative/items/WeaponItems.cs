using System.Collections.Generic;
using Cathedral.Fight;

namespace Cathedral.Game.Narrative.Items;

// ─────────────────────────────────────────────────────────────────────────────
// Abstract base for all weapon items
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Base class for all hand-held combat weapons.
/// Implements <see cref="IWeaponItem"/> so the fight system can use them as weapon mediums.
/// </summary>
public abstract class WeaponItem : Item, IWeaponItem
{
    public sealed override ItemCategory Category       => ItemCategory.Weapon;
    public sealed override string       SubcategoryKey => WeaponCategory;

    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.RightHold;
    public abstract int Level { get; }

    // Weapons are sold as part of the blacksmith's Ironwork. Goods are priced in copper across the
    // board — the other denominations are reserved for wages and larger transactions — so a
    // weapon's level maps onto ten copper a step: a level-3 warblade costs thirty, against a
    // barrel's twenty-five and a lantern's thirty.
    public override List<ItemTag> Tags    => new() { ItemTag.Ironwork };
    public override int           PriceReference => (Level < 1 ? 1 : Level) * 10;

    /// <summary>
    /// How much a weapon helps when combined with a <em>narration</em> action — a different
    /// question from <see cref="Level"/>, which is combat proficiency. A longbow is an excellent
    /// weapon and a useless lever; an axe is a middling weapon and a superb tool. So this keys off
    /// the weapon's <em>shape</em> rather than its deadliness: what could you actually do with it
    /// if you needed to cut, pry, dig or hammer something.
    /// </summary>
    public override int UsageLevel => WeaponCategory switch
    {
        "axe"         => 5,   // chopping, splitting — a working tool that happens to kill
        "pickaxe"     => 5,   // digging, prying, breaking stone
        "blunt"       => 4,   // driving stakes, breaking things open
        "long_blade"  => 3,   // cutting, levering
        "short_blade" => 3,   // the most generally useful blade for fine work
        "spear"       => 3,   // reach: poking, probing, pinning
        "shield"      => 2,   // a board is a board — shelter, digging, carrying
        "saber"       => 2,   // curved and light; poor at everything but cutting
        "bow"         => 1,   // a stave and a string
        "crossbow"    => 1,   // a mechanism, and a fragile one
        _             => 1,
    };

    /// <summary>
    /// Single weapon category key. Matches keys used in <see cref="Cathedral.Fight.FightingMedium.WeaponCategories"/>.
    /// Each concrete weapon has exactly one category.
    /// </summary>
    public abstract string WeaponCategory { get; }
}

// ─────────────────────────────────────────────────────────────────────────────
// Long blade
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>A plain arming sword — straight cross-hilted blade of serviceable iron.</summary>
public sealed class ArmingSword : WeaponItem
{
    public override string ItemId      => "arming_sword";
    public override string DisplayName => "Arming Sword";
    public override string Description => "A straight double-edged arming sword with a plain iron cross-guard";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass   Weight     => WeightClass.Medium;
    public override int     Level      => 2;
    public override string WeaponCategory => "long_blade";
}

/// <summary>A long two-handed sword of tempered steel — a warrior's weapon.</summary>
public sealed class Longsword : WeaponItem
{
    public override string ItemId      => "longsword";
    public override string DisplayName => "Longsword";
    public override string Description => "A long straight blade of tempered steel with a leather-wrapped grip and heavy pommel";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass   Weight     => WeightClass.Medium;
    public override int     Level      => 3;
    public override string WeaponCategory => "long_blade";
}

// ─────────────────────────────────────────────────────────────────────────────
// Short blade
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>A sturdy hunting knife — broad blade, bone handle, all-purpose edge.</summary>
public sealed class HuntingKnife : WeaponItem
{
    public override string ItemId      => "hunting_knife";
    public override string DisplayName => "Hunting Knife";
    public override string Description => "A thick-bladed hunting knife with a bone handle and a single-edged blade for gutting game";
    public override ItemSize Size      => ItemSize.Small;
    public override WeightClass   Weight     => WeightClass.Light;
    public override int     Level      => 1;
    public override string WeaponCategory => "short_blade";
}

/// <summary>A double-edged iron dagger — compact and lethal at close quarters.</summary>
public sealed class IronDagger : WeaponItem
{
    public override string ItemId      => "iron_dagger_weapon";
    public override string DisplayName => "Iron Dagger";
    public override string Description => "A plain double-edged iron dagger with a tapered blade and a riveted pommel";
    public override ItemSize Size      => ItemSize.Small;
    public override WeightClass   Weight     => WeightClass.Light;
    public override int     Level      => 2;
    public override string WeaponCategory => "short_blade";
}

// ─────────────────────────────────────────────────────────────────────────────
// Saber
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>A curved cavalry saber — single-edged blade with a hand guard.</summary>
public sealed class CavalrySaber : WeaponItem
{
    public override string ItemId      => "cavalry_saber";
    public override string DisplayName => "Cavalry Saber";
    public override string Description => "A curved single-edged saber with a brass hand guard and a sharkskin grip";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass   Weight     => WeightClass.Medium;
    public override int     Level      => 2;
    public override string WeaponCategory => "saber";
}

/// <summary>A broad-bladed cutlass — heavy curved sword used by soldiers and sailors.</summary>
public sealed class Cutlass : WeaponItem
{
    public override string ItemId      => "cutlass";
    public override string DisplayName => "Cutlass";
    public override string Description => "A heavy-bladed cutlass with a broad curve and a basket hilt of iron rings";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass   Weight     => WeightClass.Medium;
    public override int     Level      => 3;
    public override string WeaponCategory => "saber";
}

// ─────────────────────────────────────────────────────────────────────────────
// Blunt weapon
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>A thick wooden cudgel — simple, brutal, and always to hand.</summary>
public sealed class Cudgel : WeaponItem
{
    public override string ItemId      => "cudgel";
    public override string DisplayName => "Cudgel";
    public override string Description => "A thick knotted branch trimmed into a fighting club, heavy at the head";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass   Weight     => WeightClass.Medium;
    public override int     Level      => 1;
    public override string WeaponCategory => "blunt";
}

/// <summary>A flanged iron warhammer — head shaped to shatter bone through armour.</summary>
public sealed class Warhammer : WeaponItem
{
    public override string ItemId      => "warhammer";
    public override string DisplayName => "Warhammer";
    public override string Description => "A flanged iron warhammer with a cross-peen head and a leather-wrapped haft";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass   Weight     => WeightClass.Medium;
    public override int     Level      => 2;
    public override string WeaponCategory => "blunt";
}

// ─────────────────────────────────────────────────────────────────────────────
// Axe
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>A single-bit battle axe — heavy bearded head on a long haft.</summary>
public sealed class BattleAxe : WeaponItem
{
    public override string ItemId      => "battle_axe";
    public override string DisplayName => "Battle Axe";
    public override string Description => "A single-bit iron axe with a bearded head and an ash haft bound in leather";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass   Weight     => WeightClass.Medium;
    public override int     Level      => 2;
    public override string WeaponCategory => "axe";
}

/// <summary>A double-bitted war axe — brutal cleaving weapon favoured by northern fighters.</summary>
public sealed class WarAxe : WeaponItem
{
    public override string ItemId      => "war_axe";
    public override string DisplayName => "War Axe";
    public override string Description => "A double-bitted war axe of dark iron, the hafts grooved for grip";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass   Weight     => WeightClass.Heavy;
    public override int     Level      => 3;
    public override string WeaponCategory => "axe";
}

// ─────────────────────────────────────────────────────────────────────────────
// Pickaxe (repurposed as weapon)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>An iron war pick — the point bores through plate and bone alike.</summary>
public sealed class WarPick : WeaponItem
{
    public override string ItemId      => "war_pick";
    public override string DisplayName => "War Pick";
    public override string Description => "A single-pointed iron war pick with a hammerhead back and an oak haft";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass   Weight     => WeightClass.Medium;
    public override int     Level      => 2;
    public override string WeaponCategory => "pickaxe";
}

/// <summary>A heavy iron pickaxe repurposed for fighting — slow but devastating.</summary>
public sealed class HeavyPick : WeaponItem
{
    public override string ItemId      => "heavy_pick";
    public override string DisplayName => "Heavy Pick";
    public override string Description => "A miner's heavy iron pick, its point ground to a weapon edge";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass   Weight     => WeightClass.Heavy;
    public override int     Level      => 1;
    public override string WeaponCategory => "pickaxe";
}

// ─────────────────────────────────────────────────────────────────────────────
// Spear
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>A hunting spear — light iron point on a long ash shaft.</summary>
public sealed class HuntingSpear : WeaponItem
{
    public override string ItemId      => "hunting_spear";
    public override string DisplayName => "Hunting Spear";
    public override string Description => "A hunting spear with a leaf-shaped iron point and a smooth ash shaft";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass   Weight     => WeightClass.Medium;
    public override int     Level      => 2;
    public override string WeaponCategory => "spear";
}

/// <summary>A war spear — heavy socketed head designed to punch through shields.</summary>
public sealed class WarSpear : WeaponItem
{
    public override string ItemId      => "war_spear";
    public override string DisplayName => "War Spear";
    public override string Description => "A heavy-socketed war spear with a broad iron head and a steel-shod butt";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass   Weight     => WeightClass.Medium;
    public override int     Level      => 3;
    public override string WeaponCategory => "spear";
}

// ─────────────────────────────────────────────────────────────────────────────
// Bows
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>A short hunting bow — light self-bow of yew, quick to nock and loose.</summary>
public sealed class HuntingBow : WeaponItem
{
    public override string ItemId      => "hunting_bow";
    public override string DisplayName => "Hunting Bow";
    public override string Description => "A short self-bow of yew, unstrung and bound with waxed linen";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass   Weight     => WeightClass.Medium;
    public override int     Level      => 1;
    public override string WeaponCategory => "bow";
}

/// <summary>A powerful longbow — drawn to the ear, it strikes hard at distance.</summary>
public sealed class Longbow : WeaponItem
{
    public override string ItemId      => "longbow";
    public override string DisplayName => "Longbow";
    public override string Description => "A tall war longbow of laminated horn and sinew, the draw weight substantial";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass   Weight     => WeightClass.Medium;
    public override int     Level      => 3;
    public override string WeaponCategory => "bow";
}

// ─────────────────────────────────────────────────────────────────────────────
// Crossbows
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>A light crossbow — simple prod and nut mechanism, easy to reload.</summary>
public sealed class LightCrossbow : WeaponItem
{
    public override string ItemId      => "light_crossbow";
    public override string DisplayName => "Light Crossbow";
    public override string Description => "A light crossbow with a simple nut-and-prod mechanism and a short wooden tiller";
    public override ItemSize Size      => ItemSize.Medium;
    public override WeightClass   Weight     => WeightClass.Medium;
    public override int     Level      => 2;
    public override string WeaponCategory => "crossbow";
}

/// <summary>A heavy steel crossbow — slow to span but punches through armour.</summary>
public sealed class HeavyCrossbow : WeaponItem
{
    public override string ItemId      => "heavy_crossbow";
    public override string DisplayName => "Heavy Crossbow";
    public override string Description => "A heavy steel-prod crossbow with a windlass crank and a carved cheek-piece";
    public override ItemSize Size      => ItemSize.Large;
    public override WeightClass   Weight     => WeightClass.Heavy;
    public override int     Level      => 3;
    public override string WeaponCategory => "crossbow";
}

// ─────────────────────────────────────────────────────────────────────────────
// Shield (off-hand, equipped LeftHold)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>A round shield of iron-bossed wood — light and versatile.</summary>
public sealed class RoundShield : WeaponItem
{
    public override string ItemId        => "round_shield";
    public override string DisplayName   => "Round Shield";
    public override string Description   => "A round iron-bossed shield of limewood, the rim bound in leather";
    public override ItemSize Size        => ItemSize.Medium;
    public override WeightClass   Weight       => WeightClass.Heavy;
    public override int     Level        => 1;
    public override string WeaponCategory => "shield";
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.LeftHold;
}

/// <summary>A full-body tower shield — heavy but offers near-total cover.</summary>
public sealed class TowerShield : WeaponItem
{
    public override string ItemId        => "tower_shield";
    public override string DisplayName   => "Tower Shield";
    public override string Description   => "A tall kite-shaped shield of planked wood faced in iron plate";
    public override ItemSize Size        => ItemSize.Large;
    public override WeightClass   Weight       => WeightClass.Heavy;
    public override int     Level        => 2;
    public override string WeaponCategory => "shield";
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.LeftHold;
}
