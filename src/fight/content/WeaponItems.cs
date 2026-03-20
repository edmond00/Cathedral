using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Fight.Content;

/// <summary>
/// An iron sword — a straight double-edged blade. Can be used as a weapon medium.
/// Equips to Right Hold.
/// </summary>
public sealed class IronSword : Item, IWeaponItem
{
    public override string ItemId       => "fight_iron_sword";
    public override string DisplayName  => "Iron Sword";
    public override string Description  => "A serviceable iron blade, straight and double-edged.";
    public override float  Weight       => 1.4f;
    public override ItemSize Size       => ItemSize.Medium;
    public override List<ItemType> Types => new() { ItemType.Other };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.RightHold;
    public override List<string> OutcomeKeywords => new() { "sword", "blade", "iron" };
    public override string[] Info => new[]
    {
        "Damage: 1d6+2",
        "Weight: 1.4 kg",
        "Usable as weapon medium for blade fighting skills.",
    };
}

/// <summary>
/// A fight knife — short, fast, lightweight. Can be used as a weapon medium.
/// Equips to Right Hold or Left Hold.
/// </summary>
public sealed class FightKnife : Item, IWeaponItem
{
    public override string ItemId       => "fight_knife";
    public override string DisplayName  => "Fight Knife";
    public override string Description  => "A short sturdy blade designed for close quarters.";
    public override float  Weight       => 0.35f;
    public override ItemSize Size       => ItemSize.Small;
    public override List<ItemType> Types => new() { ItemType.Other };
    public override EquipmentAnchor? PreferredAnchor => EquipmentAnchor.RightHold;
    public override List<string> OutcomeKeywords => new() { "knife", "blade", "short" };
    public override string[] Info => new[]
    {
        "Damage: 1d4+2",
        "Weight: 0.35 kg",
        "Usable as weapon medium for blade fighting skills.",
    };
}
