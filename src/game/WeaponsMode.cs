using System;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game;

/// <summary>
/// Weapons mode: when --weapons is passed on the command line, every newly created
/// protagonist receives a starter weapon loadout (Arming Sword, Hunting Bow, Round Shield)
/// placed directly into their inventory.
/// </summary>
public static class WeaponsMode
{
    /// <summary>Whether weapons mode is active.</summary>
    public static bool IsActive { get; set; } = false;

    /// <summary>
    /// Adds starter weapons to <paramref name="protagonist"/> if weapons mode is active.
    /// Call this immediately after creating a new <see cref="Protagonist"/> instance.
    /// </summary>
    public static void ApplyIfActive(Protagonist protagonist)
    {
        if (!IsActive) return;

        var sword  = new ArmingSword();
        var bow    = new HuntingBow();
        var shield = new RoundShield();

        protagonist.AcquireItem(sword);
        protagonist.AcquireItem(bow);
        protagonist.AcquireItem(shield);

        // Equip the pair, don't just hand them over. A weapon medium is available only while
        // something is actually held (FightingSkill.IsAnyMediumAvailable reads the hold anchors),
        // so a protagonist carrying a sword in their pack still cannot use a single sword skill —
        // which made this flag unable to do the one thing it exists for.
        protagonist.Equip(EquipmentAnchor.RightHold, sword);
        protagonist.Equip(EquipmentAnchor.LeftHold,  shield);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("*** --weapons: protagonist starts with Arming Sword (right hand), " +
                          "Round Shield (left hand) and a Hunting Bow in the pack ***");
        Console.ResetColor();
    }

    /// <summary>
    /// Lifts each arm to at least level 1 when creation is accepted. The sword and the shield are
    /// medium weights, and an arm of level 0 bears only light things (<see cref="PartyMember.HoldLimit"/>):
    /// held in a weak hand they would sit there unwielded, and the flag would again fail at the one
    /// thing it is for. Applied ahead of <c>--organs</c>, which still wins when it names the arms.
    /// </summary>
    public static void StrengthenArmsIfActive(Protagonist protagonist)
    {
        if (!IsActive) return;
        foreach (var partId in new[] { "right_arm", "left_arm" })
            if (protagonist.GetOrganPartById(partId) is { } arm && arm.Score < 1)
                arm.Score = 1;
    }
}
