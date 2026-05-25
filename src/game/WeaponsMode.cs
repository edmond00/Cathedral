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

        protagonist.AcquireItem(new ArmingSword());
        protagonist.AcquireItem(new HuntingBow());
        protagonist.AcquireItem(new RoundShield());

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("*** --weapons: protagonist starts with Arming Sword, Hunting Bow, Round Shield ***");
        Console.ResetColor();
    }
}
