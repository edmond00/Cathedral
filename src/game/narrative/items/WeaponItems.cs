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
    public override List<ItemType> Types           => new() { ItemType.Other };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.RightHold;
    public abstract int Level { get; }
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
    public override float   Weight     => 1.3f;
    public override int     Level      => 2;
    public override string[] Info => new[]
    {
        "Type: Long Blade",
        "Level: 2",
        "Unlocks: Cleaving Strike, Counter Strike, Forward Lunge, Feint",
    };
}

/// <summary>A long two-handed sword of tempered steel — a warrior's weapon.</summary>
public sealed class Longsword : WeaponItem
{
    public override string ItemId      => "longsword";
    public override string DisplayName => "Longsword";
    public override string Description => "A long straight blade of tempered steel with a leather-wrapped grip and heavy pommel";
    public override ItemSize Size      => ItemSize.Large;
    public override float   Weight     => 1.8f;
    public override int     Level      => 3;
    public override string[] Info => new[]
    {
        "Type: Long Blade",
        "Level: 3",
        "Unlocks: Cleaving Strike, Counter Strike, Forward Lunge, Feint",
    };
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
    public override float   Weight     => 0.4f;
    public override int     Level      => 1;
    public override string[] Info => new[]
    {
        "Type: Short Blade",
        "Level: 1",
        "Unlocks: Snap Thrust, Needle Thrust, Parry, Deep Pierce",
    };
}

/// <summary>A double-edged iron dagger — compact and lethal at close quarters.</summary>
public sealed class IronDagger : WeaponItem
{
    public override string ItemId      => "iron_dagger_weapon";
    public override string DisplayName => "Iron Dagger";
    public override string Description => "A plain double-edged iron dagger with a tapered blade and a riveted pommel";
    public override ItemSize Size      => ItemSize.Small;
    public override float   Weight     => 0.5f;
    public override int     Level      => 2;
    public override string[] Info => new[]
    {
        "Type: Short Blade",
        "Level: 2",
        "Unlocks: Snap Thrust, Needle Thrust, Parry, Deep Pierce",
    };
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
    public override float   Weight     => 1.1f;
    public override int     Level      => 2;
    public override string[] Info => new[]
    {
        "Type: Saber",
        "Level: 2",
        "Unlocks: Snap Thrust, Feint, Needle Thrust, Counter Strike",
    };
}

/// <summary>A broad-bladed cutlass — heavy curved sword used by soldiers and sailors.</summary>
public sealed class Cutlass : WeaponItem
{
    public override string ItemId      => "cutlass";
    public override string DisplayName => "Cutlass";
    public override string Description => "A heavy-bladed cutlass with a broad curve and a basket hilt of iron rings";
    public override ItemSize Size      => ItemSize.Medium;
    public override float   Weight     => 1.4f;
    public override int     Level      => 3;
    public override string[] Info => new[]
    {
        "Type: Saber",
        "Level: 3",
        "Unlocks: Snap Thrust, Feint, Needle Thrust, Counter Strike",
    };
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
    public override float   Weight     => 1.0f;
    public override int     Level      => 1;
    public override string[] Info => new[]
    {
        "Type: Blunt Weapon",
        "Level: 1",
        "Unlocks: Smash, Crushing Blow, Heavy Strike, Mighty Swing",
    };
}

/// <summary>A flanged iron warhammer — head shaped to shatter bone through armour.</summary>
public sealed class Warhammer : WeaponItem
{
    public override string ItemId      => "warhammer";
    public override string DisplayName => "Warhammer";
    public override string Description => "A flanged iron warhammer with a cross-peen head and a leather-wrapped haft";
    public override ItemSize Size      => ItemSize.Medium;
    public override float   Weight     => 1.6f;
    public override int     Level      => 2;
    public override string[] Info => new[]
    {
        "Type: Blunt Weapon",
        "Level: 2",
        "Unlocks: Smash, Crushing Blow, Heavy Strike, Mighty Swing",
    };
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
    public override float   Weight     => 1.7f;
    public override int     Level      => 2;
    public override string[] Info => new[]
    {
        "Type: Axe",
        "Level: 2",
        "Unlocks: Chop, Heavy Strike, Cleaving Strike, Driving Lunge",
    };
}

/// <summary>A double-bitted war axe — brutal cleaving weapon favoured by northern fighters.</summary>
public sealed class WarAxe : WeaponItem
{
    public override string ItemId      => "war_axe";
    public override string DisplayName => "War Axe";
    public override string Description => "A double-bitted war axe of dark iron, the hafts grooved for grip";
    public override ItemSize Size      => ItemSize.Large;
    public override float   Weight     => 2.2f;
    public override int     Level      => 3;
    public override string[] Info => new[]
    {
        "Type: Axe",
        "Level: 3",
        "Unlocks: Chop, Heavy Strike, Cleaving Strike, Driving Lunge",
    };
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
    public override float   Weight     => 1.5f;
    public override int     Level      => 2;
    public override string[] Info => new[]
    {
        "Type: Pickaxe",
        "Level: 2",
        "Unlocks: Piercing Blow, Deep Pierce, Mighty Swing, Crushing Blow",
    };
}

/// <summary>A heavy iron pickaxe repurposed for fighting — slow but devastating.</summary>
public sealed class HeavyPick : WeaponItem
{
    public override string ItemId      => "heavy_pick";
    public override string DisplayName => "Heavy Pick";
    public override string Description => "A miner's heavy iron pick, its point ground to a weapon edge";
    public override ItemSize Size      => ItemSize.Large;
    public override float   Weight     => 2.0f;
    public override int     Level      => 1;
    public override string[] Info => new[]
    {
        "Type: Pickaxe",
        "Level: 1 (heavy and slow)",
        "Unlocks: Piercing Blow, Deep Pierce, Mighty Swing, Crushing Blow",
    };
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
    public override float   Weight     => 1.2f;
    public override int     Level      => 2;
    public override string[] Info => new[]
    {
        "Type: Spear",
        "Level: 2",
        "Unlocks: Forward Lunge, Piercing Blow, Driving Lunge, Deep Pierce",
    };
}

/// <summary>A war spear — heavy socketed head designed to punch through shields.</summary>
public sealed class WarSpear : WeaponItem
{
    public override string ItemId      => "war_spear";
    public override string DisplayName => "War Spear";
    public override string Description => "A heavy-socketed war spear with a broad iron head and a steel-shod butt";
    public override ItemSize Size      => ItemSize.Large;
    public override float   Weight     => 1.8f;
    public override int     Level      => 3;
    public override string[] Info => new[]
    {
        "Type: Spear",
        "Level: 3",
        "Unlocks: Forward Lunge, Piercing Blow, Driving Lunge, Deep Pierce",
    };
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
    public override float   Weight     => 0.8f;
    public override int     Level      => 1;
    public override string[] Info => new[]
    {
        "Type: Bow",
        "Level: 1",
        "Unlocks: Quickshot, Pinpoint Shot, Longshot",
    };
}

/// <summary>A powerful longbow — drawn to the ear, it strikes hard at distance.</summary>
public sealed class Longbow : WeaponItem
{
    public override string ItemId      => "longbow";
    public override string DisplayName => "Longbow";
    public override string Description => "A tall war longbow of laminated horn and sinew, the draw weight substantial";
    public override ItemSize Size      => ItemSize.Large;
    public override float   Weight     => 1.0f;
    public override int     Level      => 3;
    public override string[] Info => new[]
    {
        "Type: Bow",
        "Level: 3",
        "Unlocks: Quickshot, Pinpoint Shot, Longshot",
    };
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
    public override float   Weight     => 1.4f;
    public override int     Level      => 2;
    public override string[] Info => new[]
    {
        "Type: Crossbow",
        "Level: 2",
        "Unlocks: Sighted Shot, Pinpoint Shot, Deadeye Shot",
    };
}

/// <summary>A heavy steel crossbow — slow to span but punches through armour.</summary>
public sealed class HeavyCrossbow : WeaponItem
{
    public override string ItemId      => "heavy_crossbow";
    public override string DisplayName => "Heavy Crossbow";
    public override string Description => "A heavy steel-prod crossbow with a windlass crank and a carved cheek-piece";
    public override ItemSize Size      => ItemSize.Large;
    public override float   Weight     => 2.5f;
    public override int     Level      => 3;
    public override string[] Info => new[]
    {
        "Type: Crossbow",
        "Level: 3",
        "Unlocks: Sighted Shot, Pinpoint Shot, Deadeye Shot",
    };
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
    public override float   Weight       => 2.0f;
    public override int     Level        => 1;
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.LeftHold;
    public override string[] Info => new[]
    {
        "Type: Shield",
        "Level: 1",
        "Unlocks: Cover, Parry, Shield Bash",
    };
}

/// <summary>A full-body tower shield — heavy but offers near-total cover.</summary>
public sealed class TowerShield : WeaponItem
{
    public override string ItemId        => "tower_shield";
    public override string DisplayName   => "Tower Shield";
    public override string Description   => "A tall kite-shaped shield of planked wood faced in iron plate";
    public override ItemSize Size        => ItemSize.Large;
    public override float   Weight       => 5.5f;
    public override int     Level        => 2;
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.LeftHold;
    public override string[] Info => new[]
    {
        "Type: Shield",
        "Level: 2",
        "Unlocks: Cover, Parry, Shield Bash",
    };
}
